namespace StatsBro.Storage.ElasticSearch;

using Microsoft.Extensions.Options;
using Nest;
using StatsBro.Domain.Config;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using System.Threading.Tasks;


public class GeneralStatsResult
{
    public IList<KeyValuePair<string, long>> Referrers { get; set; } = null!;

    public IList<KeyValuePair<string, long>> Browsers { get; set; } = null!;

    public IList<KeyValuePair<string, long>> Oses{ get; set; } = null!;

    public IList<KeyValuePair<string, long>> IsTouchScreen { get; set; } = null!;

    public IList<KeyValuePair<string, long>> ScreenSize { get; set; } = null!;
}

public interface IEsStatsRepository
{
    Task<long> TotalUsersCountAsync(string domain, string query, CancellationToken cancellationToken);
    Task<long> TotalPageviewsCountAsync(string domain, string query, CancellationToken cancellationToken = default);
    Task<GeneralStatsResult> GeneralStatsAsync(string domain, string query, CancellationToken cancellation = default);
}

public class EsStatsRepository : IEsStatsRepository
{
    private readonly IEsFactory _esFactory;
    private readonly ESConfig _esConfig;

    public EsStatsRepository(IEsFactory esFactory, IOptions<ESConfig> esConfigOptions)
    {
        this._esFactory = esFactory;
        this._esConfig = esConfigOptions.Value;
    }

    public async Task<long> TotalUsersCountAsync(string domain, string query, CancellationToken cancellationToken = default)
    {
        var aggCardinalityName = "aggc1";
        var result = await this.Client.SearchAsync<SiteVisitData>(s =>
            s.Index(Indexing.IndexName(this._esConfig, domain))
            .Size(0)
            .Query(q => q.QueryString(qq => qq.Query(query)))
            .Aggregations(a => a.Cardinality(aggCardinalityName, c => c.Field(f => f.Hash))),
            cancellationToken
        );

        var cardinalityAgg = result.Aggregations.Cardinality(aggCardinalityName);
        if (cardinalityAgg == null)
        {
            return 0;
        }

        return (long)cardinalityAgg.Value!;        
    }

    public async Task<long> TotalPageviewsCountAsync(string domain, string query, CancellationToken cancellationToken = default)
    {
        var result = await this.Client.CountAsync<SiteVisitData>(s =>
            s.Index(Indexing.IndexName(this._esConfig, domain))
            .Query(q => 
                q.QueryString(qq => 
                    qq.Query(query))).
                Query(q => 
                    q.Bool(b => 
                        b.Filter(f => 
                            f.Bool(fb => 
                                fb.Should(fs => 
                                    fs.Term(t => t.EventName, "pageview")))))),
            cancellationToken
        );

        return result.Count;
    }

    public async Task<GeneralStatsResult> GeneralStatsAsync(string domain, string query, CancellationToken cancellation = default)
    {
        var aggNameSceenSize = "screensize";
        var aggNameBrowser = "browser";
        var aggNameOs = "os";
        var aggNameIsTouchScreen = "touchscreen";
        var aggNameReferrerDomain = "refferer";

        var result = await this.Client.SearchAsync<dynamic>(s =>
            s.Index(Indexing.IndexName(this._esConfig, domain))
            .Size(0)
            .Query(q => q.QueryString(qq => qq.Query(query)))
            .Aggregations(a => 
                a.Terms(aggNameReferrerDomain, at => at.Field("referrer.domain.keyword").Size(6))
                .Terms(aggNameOs, at => at.Field("user_agent.os.full.keyword").Size(6))
                .Terms(aggNameIsTouchScreen, at => at.Field("is_touch_screen").Size(2))
                .Terms(aggNameSceenSize, at => at.Field("screen_size.keyword").Size(6))
                .Terms(aggNameBrowser, at => at.Field("user_agent.name.keyword").Size(6))),
            cancellation);

        var reffererDomainResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameReferrerDomain));
        var osesResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameOs));
        var browsersResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameBrowser));
        var screenSizeResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameSceenSize));
        var isTouchScreenResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameIsTouchScreen), new Dictionary<string, string> { { "1", "Yes" }, { "0", "No" } });

        return new GeneralStatsResult 
        {
            Referrers = reffererDomainResult,
            Oses = osesResult,
            Browsers = browsersResult,
            IsTouchScreen = isTouchScreenResult,
            ScreenSize = screenSizeResult,
        };
    }

    private IList<KeyValuePair<string, long>> UnpackTermsBucket(TermsAggregate<string>? agg, IDictionary<string, string>? mapping = null)
    {
        if (mapping == null)
        {
            mapping = new Dictionary<string, string>();
        }

        var result = new List<KeyValuePair<string, long>>();
        if(agg != null)
        {
            foreach (var o in agg.Buckets)
            {
                var key = o.Key;
                if (mapping.ContainsKey(o.Key))
                {
                    key = mapping[o.Key];
                }

                result.Add(KeyValuePair.Create(key!, (long)o.DocCount!));
            }
        }

        return result;
    }

    private IElasticClient Client
    {
        get
        {
            return _esFactory.GetClient();
        }
    }
}
