namespace StatsBro.Processor.Console.Service;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using StatsBro.Domain.Config;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Threading.Tasks;

public interface IInitializer
{
    Task Initialize(CancellationToken cancellationToken);
}

public class Initializer : IInitializer
{
    private readonly IEsFactory _esFactory;
    private readonly IBootstrapDb _bootstrapDb;
    private readonly ESConfig _esConfig;
    private readonly ILogger<Initializer> _logger;

    public Initializer(
        IBootstrapDb bootstrapDb,
        IEsFactory esFactory, 
        IOptions<ESConfig> esOptions, 
        ILogger<Initializer> logger)
    {
        this._esFactory = esFactory;
        this._bootstrapDb = bootstrapDb;
        this._esConfig = esOptions.Value;
        this._logger = logger;
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        await this._bootstrapDb.Setup();

        // TODO: move the whole ES init to StatsBro.Storage.ES.Boostrapper or something
        var client = this._esFactory.GetClient();
        var pingResponse = await client.PingAsync(p => p.ErrorTrace(true), cancellationToken);
        this.HandleResponse(pingResponse, "ping");

        await InitPipeline(client, cancellationToken);
        await InitIndexTemplate(client, cancellationToken);
        await InitializeLifecyclePolicy(client, cancellationToken);
    }

    private async Task InitPipeline(IElasticClient esClient, CancellationToken cancellationToken)
    {
        var geoIpProcessor = new GeoIpProcessor
        {
            Field = "ip",
            TargetField = "geo",
            FirstOnly = false,
            IgnoreMissing = true,
            IgnoreFailure = true,
        };

        var userAgentProcessor = new UserAgentProcessor
        {
            Field = "source_user_agent",
            TargetField = "user_agent",
            IgnoreMissing = true,
            IgnoreFailure = true,
        };

        var uriPartsProcessor = new UriPartsProcessor
        {
            Field = "original_url",
            TargetField = "url",
            IgnoreFailure = true,
            KeepOriginal = false,
        };

        var referrerPartsProcessor = new UriPartsProcessor
        {
            Field = "original_referrer",
            TargetField = "referrer",
            IgnoreFailure = true,
            KeepOriginal = false,            
        };

        // TODO: make sure kv result is mapped as `flattened`: https://www.elastic.co/guide/en/elasticsearch/reference/current/flattened.html
        var kvUrlQueryStep1 = new KeyValueProcessor
        {
            Field = "url.query",
            FieldSplit = "&",
            ValueSplit = "=",
            TargetField = "url.query_params",
            IgnoreFailure = true,
            IgnoreMissing = true,
            StripBrackets = true,
        };

        // remove useragent and original field which is added by UserAgentProcessor
        var removeFieldsProcessor = new RemoveProcessor
        {
            Field = new[] { 
                "source_user_agent", 
                "user_agent.original",
                "ip", 
                "original_url", 
                "original_referrer", 
                "url.password",
                "url.username",
                "url.user_info",
                "referrer.password",
                "referrer.username",
                "referrer.user_info"
            },
            IgnoreFailure = true,
            IgnoreMissing = true
        };

        var putPipelineResponse = await esClient
            .Ingest
            .PutPipelineAsync(
                _esConfig.PipelineNameProcessing, r => {
                    r.Processors(new List<IProcessor> { geoIpProcessor, userAgentProcessor, uriPartsProcessor, referrerPartsProcessor, kvUrlQueryStep1, removeFieldsProcessor });
                    r.Version(_esConfig.PipelineVersion);
                    return r;
                }, cancellationToken);

        this.HandleResponse(putPipelineResponse, "put pieline");
    }

    private async Task InitIndexTemplate(IElasticClient esClient, CancellationToken cancellationToken)
    {
        var getTemplateResponse = await esClient.Indices.GetTemplateV2Async(new GetIndexTemplateV2Request($"{this._esConfig.IndexPrefix}{this._esConfig.IndexTemplateName}"), cancellationToken);
        if (getTemplateResponse.ApiCall!.HttpStatusCode.HasValue &&
            getTemplateResponse.ApiCall.HttpStatusCode! == (int)System.Net.HttpStatusCode.NotFound)
        {
            this._logger.LogInformation("Index template not found in ES, need to create one.");

            var properties = new Dictionary<PropertyName, IProperty>
            {
                { new PropertyName("@timestamp"), new DateProperty() },
                { new PropertyName("hash"), new KeywordProperty() },
                { new PropertyName("domain"), new KeywordProperty() },
                { new PropertyName("lang"), new KeywordProperty() },
                { new PropertyName("event"), new KeywordProperty() },
                { new PropertyName("url.query_params"), new FlattenedProperty() },
            };

            var r2 = new PutIndexTemplateV2Request($"{this._esConfig.IndexPrefix}{this._esConfig.IndexTemplateName}")
            {
                IndexPatterns = new List<string>{ Indexing.IndexName(this._esConfig, "*") },
                Version = _esConfig.IndexTemplateVersion,
                Template = new Template 
                { 
                    Settings = new IndexSettings 
                    { 
                        NumberOfShards = 1,
                        NumberOfReplicas = 1                        
                    },
                    Mappings = new TypeMapping
                    {
                        Properties = new Properties(properties)
                    }
                },
                DataStream = new Nest.DataStream { Hidden = false }
            };

            var putIndexTemplateResponse = await esClient.Indices.PutTemplateV2Async(r2, cancellationToken);
            this.HandleResponse(putIndexTemplateResponse, "put index-template");
        }
        else
        {
            this.HandleResponse(getTemplateResponse, "index-template");
        }
    }

    private async Task InitializeLifecyclePolicy(IElasticClient esClient, CancellationToken cancellationToken)
    {
        var request = new GetLifecycleRequest(this._esConfig.LifecyclePolicyName);
        var getLifecycleResponse = await esClient.IndexLifecycleManagement.GetLifecycleAsync(request, cancellationToken);

        if (getLifecycleResponse.ApiCall!.HttpStatusCode.HasValue &&
           getLifecycleResponse.ApiCall.HttpStatusCode! == (int)System.Net.HttpStatusCode.NotFound)
        {
            this._logger.LogWarning("Lifecycle Policy is missing !!!");
        }
    }

    private void HandleResponse(ResponseBase? response, string stepName)
    {
        if (response == null)
        {
            return;
        }

        if (response.ServerError != null)
        {
            _logger.LogWarning("We have warnings while initializing in {step} ElasticSearch: status: {status}, error: {error}", stepName, response.ServerError.Status, response.ServerError.Error.ToString());
        }

        if (!response.IsValid)
        {
            var message = string.Empty;
            if (response.OriginalException != null)
            {
                message = response.OriginalException.Message;
            }

            _logger.LogError("ElasticSearch [{step}] failed: {message}", stepName, message);
            throw new InitializationException($"Was not able to [{stepName}] in ElasticSearch, something is wrong. {message}");
        }
    }
}
