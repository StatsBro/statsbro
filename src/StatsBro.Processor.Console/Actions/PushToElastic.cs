namespace StatsBro.Processor.Console.Actions;

using Microsoft.Extensions.Options;
using StatsBro.Domain.Config;
using StatsBro.Domain.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StatsBro.Storage.ElasticSearch;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using StatsBro.Domain.Helpers;

public interface IPushToElastic
{
    public Task Act(SiteVisitData[] contentItems);
}

public class PushToElastic : IPushToElastic
{
    private readonly IEsFactory _esFactory;
    private readonly ESConfig _esConfig;
    private readonly ILogger<PushToElastic> _logger;
    private readonly ObservableCollection<SiteVisitData>  _buffer = new();

    public PushToElastic(IEsFactory esFactory, IOptions<ESConfig> esConfigOptions, ILogger<PushToElastic> logger)
    {
        this._esFactory = esFactory;
        this._esConfig = esConfigOptions.Value;
        this._logger = logger;

        //Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(_buffer, "CollectionChanged")
        // .Select(change => change.EventArgs.NewItems)
        // .Buffer(TimeSpan.FromSeconds(2), 100)
        // .Where(_ => _.Any())
        // .Select(x => x.OfType<SiteVisitData>())
        // .Subscribe(async d => await IndexData(d));
    }

    // TODO: if we need sufisticated logic on indexing: https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/indexing-documents.html#_advanced_bulk_indexing
    public async Task Act(SiteVisitData[] contentItems)
    {
        //foreach (var item in contentItems)
        //{
        //    this._buffer.Add(item);
        //}

        var client = _esFactory.GetClient();
        //var bulkResponse = await client
        //        .BulkAsync(b => b
        //        .Index("statsbro-demo")
        //        .IndexMany(contentToIndex, (descriptor, v) => descriptor.Pipeline(this._esConfig.PipelineNameProcessing)));


        //if(!bulkResponse.IsSuccess())
        //{
        //    this._logger.LogWarning($"Failed builkInsert to ES: {bulkResponse.Warnings}");
        //}
        await Task.Delay(10);
        if (contentItems.Length > 0)
        {
            List<Task<Nest.BulkResponse>> bulkResponseTasks = new();
            foreach(var group in contentItems.GroupBy(x => x.Domain))
            {
                var responseTask = client.BulkAsync(d =>
                {
                    d.Index(Indexing.IndexName(this._esConfig, group.Key))
                    .Pipeline(this._esConfig.PipelineNameProcessing)
                    .CreateMany(group);

                    return d;
                });

                bulkResponseTasks.Add(responseTask);
            }

            try
            {
                await Task.WhenAll(bulkResponseTasks).ContinueWith(t =>
                {
                    var indexingResponses = t.Result;
                    if (indexingResponses == null)
                    {
                        return;
                    }

                    for (int i = 0; i < indexingResponses.Length; i++)
                    {
                        var response = indexingResponses[i];
#if DEBUG
                        this._logger.LogInformation("{message}", response.DebugInformation);
#endif
                        if (response != null && response.ApiCall.HttpStatusCode > 399)
                        {
                            this._logger.LogInformation("Failed indexing document, response {code}", response.ApiCall.HttpStatusCode);
                        }

                        if (response?.Took > 1000)
                        {
                            this._logger.LogInformation("It took more than 1 sec to index in ES: {duration} miliseconds", response.Took);
                        }
                    }
                });
            }
            catch (Exception exc)
            {
                this._logger.LogError(exc, "Failed indexing to ES");
            }
                        

            //            var response = await client
            //                .IndexAsync(
            //                    contentItems[0],
            //                    i => i.Index($"statsbro-{contentItems[0].Domain}").Pipeline(this._esConfig.PipelineNameProcessing)
            //                );

            //#if DEBUG
            //            this._logger.LogInformation("{message}", response.DebugInformation);
            //#endif

            //            if (response != null && (!response.IsValid || !response.ApiCall.Success))
            //            {
            //                this._logger.LogInformation("Failed indexing document, response {code}", response.ApiCall.HttpStatusCode);
            //            }
        }
    }

    //private async Task IndexData(IEnumerable<SiteVisitData> visits)
    //{
    //    await Task.Delay(100);
    //    var l = visits.ToList();
    //    var count = l.Count;
    //    Console.WriteLine(count);
    //}
}
