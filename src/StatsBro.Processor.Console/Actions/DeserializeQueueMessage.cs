namespace StatsBro.Processor.Console.Actions;

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
        try
        {
            // TODO: add exceptions handing and filtering on fake data 
            var messageContent = Encoding.UTF8.GetString(messageBytes.ToArray());
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
            this._logger.LogDebug("{memberName}: Problem deserializing message from queue {message}", memberName, exc.Message);

        }

        return null;
    }
}
