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
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

public interface IEventPayloadDeserializer
{
    SiteVisitData? Act(ReadOnlyMemory<byte> bodyBytes, [CallerMemberName] string memberName = "");
}

public class DeserializeQueueMessage : IEventPayloadDeserializer
{
    private readonly JsonSerializerOptions jsonSerializationOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ILogger<DeserializeQueueMessage> _logger;

    public DeserializeQueueMessage(ILogger<DeserializeQueueMessage> logger)
    {
        this._logger = logger;
    }

    public SiteVisitData? Act(ReadOnlyMemory<byte> messageBytes, [CallerMemberName] string memberName = "")
    {
        string messageContent = string.Empty;
        try
        {
            // TODO: add exceptions handing and filtering on fake data 
            messageContent = Encoding.UTF8.GetString(messageBytes.ToArray());
            var eventPayload = JsonSerializer.Deserialize<EventPayload>(messageContent)!;
            var eventPayloadContent = JsonSerializer.Deserialize<EventPayloadContent>(eventPayload.Content, options: jsonSerializationOptions)!;

            var result = new SiteVisitData
            {
                IP = eventPayload.IP,
                Timestamp = eventPayload.Timestamp,
                Lang = eventPayloadContent.Lang,
                Referrer = eventPayloadContent.Referrer,
                TouchPoints = eventPayloadContent.TouchPoints,
                Url = eventPayloadContent.Url,
                UserAgent = eventPayloadContent.UserAgent,
                WindowHeight = eventPayloadContent.WindowHeight,
                WindowWidth = eventPayloadContent.WindowWidth,
                EventName = eventPayloadContent.EventName,
                ScriptVersion = eventPayloadContent.ScriptVersion,
            };

            return result;
        }
        catch (Exception exc)
        {
            this._logger.LogDebug("{memberName}: Problem deserializing message from queue {message}, content: {content}", memberName, exc.Message, messageContent);

        }

        return null;
    }
}
