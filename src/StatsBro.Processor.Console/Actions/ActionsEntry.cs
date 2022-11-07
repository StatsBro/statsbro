﻿namespace StatsBro.Processor.Console.Actions;

using StatsBro.Domain.Models;
using System;
using System.Threading.Tasks;

public interface IActionsEntry
{
    Task Act(ReadOnlyMemory<byte> inputBody);
}

internal partial class ActionsEntry : IActionsEntry
{
    //private readonly TransformBlock<ReadOnlyMemory<byte>, EventPayloadContent> _pipelineEntry;
    private readonly IPushToElastic _pushToElastic;
    private readonly IEventPayloadDeserializer _payloadDeserializer;
    private readonly IEventPyloadFilter _eventPyloadFilter;
    private readonly IContentProcessor _eventPayloadProcessor;

    // TODO: in case performace issues use Disruptor https://lmax-exchange.github.io/disruptor/user-guide/index.html
    // .net implementation: https://github.com/disruptor-net/Disruptor-net + description: https://medium.com/@ocoanet/improving-net-disruptor-performance-part-1-bc27995bca48
    // https://michaelscodingspot.com/pipeline-implementations-csharp-3/
    public ActionsEntry(
        IPushToElastic pushToElastic,
        IEventPayloadDeserializer payloadDeserializer,
        IEventPyloadFilter eventPyloadFilter,
        IContentProcessor eventPayloadProcessor
        )
    {
        this._pushToElastic = pushToElastic;
        this._payloadDeserializer = payloadDeserializer;
        this._eventPyloadFilter = eventPyloadFilter;
        this._eventPayloadProcessor = eventPayloadProcessor;

        //// TODO: this is crap which dies after a while
        //var deserializeQueueMessageAction = new TransformBlock<ReadOnlyMemory<byte>, EventPayloadContent?>(this._payloadDeserializer.Act);
        //var filterAction = new TransformBlock<EventPayloadContent?, EventPayloadContent?>(this._eventPyloadFilter.Act);
        //var batchBlock = new BatchBlock<EventPayloadContent>(batchSize: 100); // TODO: fix it as this is dummy approach
        //var pushToElasticBatchAction = new ActionBlock<EventPayloadContent[]>(this._pushToElastic.Act);

        ////TODO: add here buffer which will be releases based on: buffor is full | timeout
        //deserializeQueueMessageAction.LinkTo(filterAction);
        //filterAction.LinkTo(batchBlock);
        //batchBlock.LinkTo(pushToElasticBatchAction);

        //this._pipelineEntry = deserializeQueueMessageAction;
        //Console.WriteLine(this._pipelineEntry); // TODO: :)
    }
    public async Task Act(ReadOnlyMemory<byte> inputBody)
    {
        //var o = new System.Reactive.AnonymousObservable<string>();
        //o.TimeInterval
        //await
        //        AsyncObservable
        //            .Interval(TimeSpan.FromMilliseconds(300))
        //            .Buffer(TimeSpan.FromSeconds(1))
        //            .Select(xs => string.Join(", ", xs))
        //            .SubscribeAsync(Print<string>()); // TODO: Use ForEachAsync.

        //await _pipelineEntry.SendAsync(inputBody);

        await JustDoIt(inputBody);
    }

    private async Task JustDoIt(ReadOnlyMemory<byte> inputBody)
    {
        var deserialized = this._payloadDeserializer.Act(inputBody);
        var filtered = this._eventPyloadFilter.Act(deserialized);
        if(filtered == null)
        {
            return;
        }

        var processedPaylod = this._eventPayloadProcessor.Act(filtered);
        await this._pushToElastic.Act(new SiteVisitData[] { processedPaylod });
    }
}
