using System;

namespace StatsBro.Domain.Models
{
    public class SiteVisitData
    {
        [Nest.PropertyName("original_url")]
        public string Url { get; set; } = null!;
        
        [Nest.PropertyName("original_referrer")]
        public string Referrer { get; set; } = null!;

        [Nest.PropertyName("window_width")]
        public int WindowWidth { get; set; }

        [Nest.PropertyName("window_height")]
        public int WindowHeight { get; set; }

        [Nest.PropertyName("touch_points")]
        public int TouchPoints { get; set; }

        [Nest.PropertyName("lang")]
        [Nest.Keyword]
        public string Lang { get; set; } = null!;

        [Nest.PropertyName("source_user_agent")]
        public string UserAgent { get; set; } = null!;

        [Nest.PropertyName("hash")]
        [Nest.Keyword]
        public string Hash { get; set; } = null!;

        [Nest.Keyword]
        [Nest.PropertyName("domain")]
        public string Domain { get; set; } = null!;

        [Nest.PropertyName("ip")]
        public string? IP { get; set; }

        [Nest.PropertyName("event")]
        [Nest.Keyword]
        public string? EventName { get; set; }

        [Nest.PropertyName("@timestamp")]
        [Nest.Date]
        public DateTime Timestamp { get; set; }

        [Nest.PropertyName("is_touch_screen")]
        public bool IsTouchScreen { get; set; }

        [Nest.Keyword]
        [Nest.PropertyName("screen_size")]
        public string ScreenSize { get; set; } = null!;

        [Nest.PropertyName("script_version")]
        public int ScriptVersion { get; set; }
    }
}