using System;
using System.Collections.Generic;

namespace StatsBro.Domain.Models.Stats
{
    public class HistogramStatsResult
    {
        public IList<KeyValuePair<DateTimeOffset, long>> ViewsOverTime { get; set; } = null!;
        public IList<KeyValuePair<DateTimeOffset, long>> UsersOverTime { get; set; } = null!;
        public IList<KeyValuePair<DateTimeOffset, long>> EngagedUsersOverTime { get; set; } = null!;
        public IList<KeyValuePair<DateTimeOffset, long>> AvgTimeOnSiteOverTime { get; set; } = null!;
    }
}
