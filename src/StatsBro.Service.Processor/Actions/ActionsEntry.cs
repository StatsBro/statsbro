/* Copyright StatsBro.io and/or licensed to StatsBro.io under one
 * or more contributor license agreements.
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the Server Side Public License, version 1

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * Server Side Public License for more details.

 * You should have received a copy of the Server Side Public License
 * along with this program. If not, see
 * <https://github.com/StatsBro/statsbro/blob/main/LICENSE>.
 */
﻿namespace StatsBro.Service.Processor.Actions;

using Microsoft.Extensions.Logging;
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
    private readonly IReferrerTimeSpentUpdate _timeSpenUpdate;
    private readonly ILogger<ActionsEntry> _logger;   

    // TODO: in case performace issues use Disruptor https://lmax-exchange.github.io/disruptor/user-guide/index.html
    // .net implementation: https://github.com/disruptor-net/Disruptor-net + description: https://medium.com/@ocoanet/improving-net-disruptor-performance-part-1-bc27995bca48
    // https://michaelscodingspot.com/pipeline-implementations-csharp-3/
    public ActionsEntry(
        IPushToElastic pushToElastic,
        IEventPayloadDeserializer payloadDeserializer,
        IEventPyloadFilter eventPyloadFilter,
        IContentProcessor eventPayloadProcessor,
        IReferrerTimeSpentUpdate timeSpentUpdate,
        ILogger<ActionsEntry> logger
        )
    {
        this._pushToElastic = pushToElastic;
        this._payloadDeserializer = payloadDeserializer;
        this._eventPyloadFilter = eventPyloadFilter;
        this._eventPayloadProcessor = eventPayloadProcessor;
        this._timeSpenUpdate = timeSpentUpdate;
        this._logger = logger;

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
        // TODO: implement it as chain of responsibility
        var deserialized = this._payloadDeserializer.Act(inputBody);
        var filtered = this._eventPyloadFilter.Act(deserialized);
        if(filtered == null)
        {
            this._logger.LogDebug("message dropped by filter {domain}", deserialized?.Domain);
            return;
        }

        var processedPaylod = this._eventPayloadProcessor.Act(filtered);
        await this._pushToElastic.Act(new SiteVisitData[] { processedPaylod });
        await this._timeSpenUpdate.Act(processedPaylod);
    }
}
