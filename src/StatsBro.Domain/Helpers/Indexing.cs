using StatsBro.Domain.Config;

namespace StatsBro.Domain.Helpers
{
    public class Indexing
    {
        public static string IndexName(ESConfig esConfig, string domain)
        {
            return $"{esConfig.IndexPrefix}statsbro-{domain}";
        }
    }
}
