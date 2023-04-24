using System.Collections.Generic;

namespace StatsBro.Domain.Models.Stats
{
    public class TopStatsResult
    {
        public IList<KeyValuePair<string, long>> Countries { get; set; } = null!;

        public IList<KeyValuePair<string, long>> Cities { get; set; } = null!;

        public IList<KeyValuePair<string, long>> Languages { get; set; } = null!;

        public IList<KeyValuePair<string, long>> Pages { get; set; } = null!;
        public IList<KeyValuePair<string, long>> EntryPages { get; set; } = null!;

        public IList<KeyValuePair<string, long>> Campaigns { get; set; } = null!;

        public IList<KeyValuePair<string, long>> Engaging { get; set; } = null!;
    }
}
