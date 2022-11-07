using Elastic.Clients.Elasticsearch.Serialization;
using System;

namespace StatsBro.Domain.Models
{
    public class EventPayload
    {
        public string Content { get; set; } = null!;

        public string IP { get; set; } = null!;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }


    public class EventPayloadContent
    {
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string Url { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("r")]
        public string Referrer { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("ww")]
        public int WindowWidth { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("wh")]
        public int WindowHeight { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("tp")]
        public int TouchPoints { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("l")]
        [StringEnum]
        public string Lang { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("ua")]
        public string UserAgent { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("e")]
        public string EventName { get; set; } = null!;


        [System.Text.Json.Serialization.JsonPropertyName("v")]
        public int ScriptVersion { get; set; } = 0;
    }
}