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
using System;
using System.Collections.Generic;

namespace StatsBro.Domain.Models
{
    public class StatsData
    {
        public StatsSummary Summary { get; set; } = null!;
        public IList<KeyValuePair<DateTimeOffset, long>> UsersOverTime { get; set; } = null!;
        public IList<KeyValuePair<DateTimeOffset, long>> EngagedUsersOverTime { get; set; } = null!;
        public IList<KeyValuePair<DateTimeOffset, long>> PageViewsOverTime { get; set; } = null!;
        public IList<KeyValuePair<DateTimeOffset, long>> AverageTimeOnSiteOverTime { get; set; } = null!;
        public IList<KeyValuePair<string, long>> TrafficSource { get; set; } = null!;
        public IList<KeyValuePair<string, long>> Browsers { get; set; } = null!;
        public IList<KeyValuePair<string, long>> OSs { get; set; } = null!;
        public IList<KeyValuePair<string, long>> TouchScreens { get; set; } = null!;
        public IList<KeyValuePair<string, long>> ScreenSizes { get; set; } = null!;
        public IList<KeyValuePair<string, long>> Campaigns { get; set; } = null!;
        public IList<KeyValuePair<string, long>> EntryPages { get; set; } = null!;
        public IList<KeyValuePair<string, long>> Languages { get; set; } = null!;
        public IList<KeyValuePair<string, long>> PagesWithMostEngagement { get; set; } = null!;
        public IList<KeyValuePair<string, long>> PageViews { get; set; } = null!;
        public IList<KeyValuePair<string, long>> Countries { get; set; } = null!;
        public IList<KeyValuePair<string, long>> Cities { get; set; } = null!;
        public IList<KeyValuePair<string, long>> EventNames { get; set; } = null!;
    }

    public class StatsSummary
    {
        public long TotalUsersCount { get; set; }
        public int? TotalUsersChange { get; set; }
        public long TotalEngagedUsersCount { get; set; }
        public int? TotalEngagedUsersChange { get; set; }
        public long TotalPageViews { get; set; }
        public long TotalCustomEvents { get; set; }
        public int? TotalPageViewChange { get; set; }
        public TimeSpan AverageVisitLength { get; set; }
        public int? AverageVisitLengthChange { get; set; }
    }
}
