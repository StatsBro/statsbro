using System.Collections.Generic;

namespace StatsBro.Domain.Models.Stats
{
    public class GeneralStatsResult
    {
        public IList<KeyValuePair<string, long>> Referrers { get; set; } = null!;

        public IList<KeyValuePair<string, long>> Browsers { get; set; } = null!;

        public IList<KeyValuePair<string, long>> Oses { get; set; } = null!;

        public IList<KeyValuePair<string, long>> IsTouchScreen { get; set; } = null!;

        public IList<KeyValuePair<string, long>> ScreenSize { get; set; } = null!;

        public IList<KeyValuePair<string, long>> EventNames { get; set; } = null!;
    }
}
