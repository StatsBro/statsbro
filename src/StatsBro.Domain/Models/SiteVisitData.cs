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
﻿using System;

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