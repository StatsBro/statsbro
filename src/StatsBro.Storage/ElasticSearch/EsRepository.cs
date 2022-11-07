namespace StatsBro.Storage.ElasticSearch;

using Microsoft.Extensions.Options;
using Nest;
using StatsBro.Domain.Config;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IEsRepository
{
    Task<IDictionary<string, bool>> HasDomainDocumentsAsync(IList<string> domains, TimeSpan timeSpan);
    
}

public class EsRepository : IEsRepository
{
    private readonly IEsFactory _esFactory;
    private readonly ESConfig _esConfig;

    public EsRepository(IEsFactory esFactory, IOptions<ESConfig> esConfigOptions)
    {
        this._esFactory = esFactory;
        this._esConfig = esConfigOptions.Value;
    }

    public async Task<IDictionary<string, bool>> HasDomainDocumentsAsync(IList<string> domains, TimeSpan timeSpan)
    {
        var result = new Dictionary<string, bool>();
        var request = new MultiSearchRequest() { Operations = new Dictionary<string, ISearchRequest>() };
        foreach (var domain in domains)
        {
            var searchRequest = new SearchRequest(Indexing.IndexName(_esConfig, domain))
            {
                Query = new DateRangeQuery { Field = "@timestamp", GreaterThanOrEqualTo = DateMath.Anchored(DateTime.UtcNow.AddDays(-7)) },
                Size = 0,
            };

            request.Operations.Add(domain, searchRequest);
            result[domain] = false;
        }

        var searchResponse = await this.Client.MultiSearchAsync(request);
        foreach (var domain in domains)
        {
            var r = searchResponse.GetResponse<object>(domain);
            if (r != null && r.Total > 0)
            {
                result[domain] = true;
            }
        }

        return result;
    }

    private IElasticClient Client
    {
        get
        {
            return _esFactory.GetClient();
        }
    }
}