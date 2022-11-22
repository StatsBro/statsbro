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
﻿namespace StatsBro.Storage.ElasticSearch;

using Microsoft.Extensions.Options;
using Nest;
using StatsBro.Domain.Config;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using System.Threading.Tasks;


public class HistogramStatsResult
{
    public IList<KeyValuePair<DateTimeOffset, long>> ViewsOverTime { get; set; } = null!;
    public IList<KeyValuePair<DateTimeOffset, long>> UsersOverTime { get; set; } = null!;
    public IList<KeyValuePair<DateTimeOffset, long>> EngagedUsersOverTime { get; set; } = null!;
}

public class GeneralStatsResult
{
    public IList<KeyValuePair<string, long>> Referrers { get; set; } = null!;

    public IList<KeyValuePair<string, long>> Browsers { get; set; } = null!;

    public IList<KeyValuePair<string, long>> Oses{ get; set; } = null!;

    public IList<KeyValuePair<string, long>> IsTouchScreen { get; set; } = null!;

    public IList<KeyValuePair<string, long>> ScreenSize { get; set; } = null!;
}

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

public interface IEsStatsRepository
{
    Task<(long, long)> TotalUsersCountAsync(string domain, string query, CancellationToken cancellationToken);
    Task<long> TotalPageviewsCountAsync(string domain, string query, CancellationToken cancellationToken = default);
    Task<HistogramStatsResult> HistogramStatsAsync(string domain, string query, CancellationToken cancellationToken = default);
    Task<GeneralStatsResult> GeneralStatsAsync(string domain, string query, CancellationToken cancellationToken = default);
    Task<TopStatsResult> TopStatsAsync(string domain, string query, CancellationToken cancellationToken = default);
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

    public async Task<(long, long)> TotalUsersCountAsync(string domain, string query, CancellationToken cancellationToken = default)
    {
        var aggCardinalityName = "aggc1";
        var aggFilterEngName = "agg_eng";
        var aggCardinalityEngName = "aggc2";

        var result = await this.Client.SearchAsync<SiteVisitData>(s =>
            s.Index(Indexing.IndexName(this._esConfig, domain))
            .Size(0)
            .Query(q => q.QueryString(qq => qq.Query(query)))
            .Aggregations(a => a
                .Cardinality(aggCardinalityName, c => c.Field(f => f.Hash))
                .Filter(aggFilterEngName, at => at.Filter(q => q.Term(t => t.Field("referrer.domain").Value(domain)))
                    .Aggregations(a => a.Cardinality(aggCardinalityEngName, c => c.Field(f => f.Hash)))
                    )
            ),
            cancellationToken
        );

        var totalCount = (long)(result.Aggregations.Cardinality(aggCardinalityName)?.Value ?? 0);
        var totalEngagedCount = (long)(result.Aggregations.Filter(aggFilterEngName)?.Cardinality(aggCardinalityEngName)?.Value ?? 0);

        return (totalCount, totalEngagedCount);
    }

    public async Task<long> TotalPageviewsCountAsync(string domain, string query, CancellationToken cancellationToken = default)
    {
        var result = await this.Client.CountAsync<SiteVisitData>(s =>
            s.Index(Indexing.IndexName(this._esConfig, domain))
            .Query(q => q.Bool(qb => qb.Must(qm => qm.QueryString(qs => qs.Query(query)) && qm.Bool(b =>
                        b.Filter(f =>
                            f.Bool(fb =>
                                fb.Should(fs =>
                                    fs.Term(t => t.EventName, "pageview")))))))),
            cancellationToken
        );

        return result.Count;
    }

    public async Task<GeneralStatsResult> GeneralStatsAsync(string domain, string query, CancellationToken cancellationToken = default)
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
            cancellationToken);

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

    public async Task<TopStatsResult> TopStatsAsync(string domain, string query, CancellationToken cancellationToken = default)
    {
        var aggNamePages = "popular_pages";
        var aggNameCountries = "popular_countries";
        var aggNameCities = "popular_cities";
        var aggNameLang = "popular_lang";
        var aggNamePopularCampaigns = "popular_campaigns";
        var aggNameEntryPagesFilter = "entry_pages";
        var aggNameMostEngagingPages = "most_engaging";


        var result = await this.Client.SearchAsync<dynamic>(s =>
           s.Index(Indexing.IndexName(this._esConfig, domain))
           .Size(0)
           .Query(q => q.QueryString(qq => qq.Query(query)))
           .Aggregations(a =>
               a.Terms(aggNamePages, at => at.Field("url.path.keyword").Size(10))
                .Terms(aggNameCountries, at => at.Field("geo.country_name.keyword").Size(10))
                .Terms(aggNameCities, at => at.Field("geo.city_name.keyword").Size(10))
                .Terms(aggNameLang, at => at.Field("lang").Size(10))
                .Terms(aggNamePopularCampaigns, at => at.Field("url.query_params.utm_campaign").Size(10))
                // to get entry pages we first filter docs to have referrer different than own domain
                .Filter(aggNameEntryPagesFilter, at => at.Filter(q => 
                    q.Bool(b => 
                        b.Should(qs => qs.MatchAll()).MustNot(qs => qs.Term(t => t.Field("referrer.domain").Value(domain)))
                        )
                    )
                    .Aggregations(a => a.Terms(aggNamePages, at => at.Field("url.path.keyword").Size(10)))
                    )
                // to get most engaging pages we get top reffering within site domain
                .Filter(aggNameMostEngagingPages, at => at.Filter(q => q.Term(t => t.Field("referrer.domain").Value(domain)))
                    .Aggregations(a => a.Terms(aggNamePages, at => at.Field("refferer.url.path").Size(10)))
                    )    
                )
           ,
           cancellationToken);

        var popularPages = UnpackTermsBucket(result.Aggregations.Terms(aggNamePages));
        var popularCities = UnpackTermsBucket(result.Aggregations.Terms(aggNameCities));
        var popularCountries = UnpackTermsBucket(result.Aggregations.Terms(aggNameCountries));
        var popularLanguages = UnpackTermsBucket(result.Aggregations.Terms(aggNameLang));
        var popularCampaigns = UnpackTermsBucket(result.Aggregations.Terms(aggNamePopularCampaigns));
        var popularEntryPages = UnpackTermsBucket(result.Aggregations.Filter(aggNameEntryPagesFilter)?.Terms(aggNamePages));
        var mostEngagingPages = UnpackTermsBucket(result.Aggregations.Filter(aggNameMostEngagingPages)?.Terms(aggNamePages));

        return new TopStatsResult 
        {
            Pages = popularPages,
            Cities = popularCities,
            Countries = popularCountries,
            Languages = popularLanguages,
            Campaigns = popularCampaigns,
            EntryPages = popularEntryPages,
            Engaging = mostEngagingPages,
        };
    }

    public async Task<HistogramStatsResult> HistogramStatsAsync(string domain, string query, CancellationToken cancellationToken = default)
    {
        var aggNameViewsOverTime = "views_over_time";
        var aggNameUsersOverTime = "users_over_time";
        var aggNameSubUsersOverTimeCardinailty = "sub_users_over_time";
        var aggNameFilterEngaged = "filter_engaged";
        var suggestedBucketsCount = 20;

        var result = await this.Client.SearchAsync<dynamic>(s =>
           s.Index(Indexing.IndexName(this._esConfig, domain))
           .Size(0)
           .Query(q => q.QueryString(qq => qq.Query(query)))
           .Aggregations(a =>
               a.AutoDateHistogram(aggNameViewsOverTime, at => at.Field("@timestamp").Buckets(suggestedBucketsCount))
               .AutoDateHistogram(aggNameUsersOverTime, at => 
                    at.Field("@timestamp").Buckets(suggestedBucketsCount)
                        .Aggregations(suba => 
                            suba.Cardinality(aggNameSubUsersOverTimeCardinailty, at => at.Field("hash"))
                            )
                        )
               .Filter(aggNameFilterEngaged, at => at.Filter(q => q.Term(t => t.Field("referrer.domain").Value(domain)))
                    .Aggregations(a => a.AutoDateHistogram(aggNameUsersOverTime, at =>
                    at.Field("@timestamp").Buckets(suggestedBucketsCount)
                        .Aggregations(suba =>
                            suba.Cardinality(aggNameSubUsersOverTimeCardinailty, at => at.Field("hash"))
                            )
                        )
                    )
               )
                ),
           cancellationToken);

        var viewsOverTime = UnpackHistogramBucket(result.Aggregations.AutoDateHistogram(aggNameViewsOverTime));
        var usersOverTime = UnpackHistogramBucket(result.Aggregations.AutoDateHistogram(aggNameUsersOverTime), aggNameSubUsersOverTimeCardinailty);
        // TODO this is a bit risky cause if users and eng users get adjusted to different intervals then chart will be shit
        // Maybe we need to do users first and then a regular date hist over engaged with a set interval that we got from users auto histogram
        var engagedOverTime = UnpackHistogramBucket(result.Aggregations.Filter(aggNameFilterEngaged)?.AutoDateHistogram(aggNameUsersOverTime), aggNameSubUsersOverTimeCardinailty);

        return new HistogramStatsResult
        {
            ViewsOverTime = viewsOverTime,
            UsersOverTime = usersOverTime,
            EngagedUsersOverTime = engagedOverTime
        };
    }

    private IList<KeyValuePair<DateTimeOffset, long>> UnpackHistogramBucket(AutoDateHistogramAggregate? agg, string? subKey = null)
    {
        var result = new List<KeyValuePair<DateTimeOffset, long>>();

        if (agg != null)
        {
            foreach (var o in agg.Buckets)
            {
                var val = subKey == null ? (long)o.DocCount! : (long)o.ValueCount(subKey).Value!;
                result.Add(KeyValuePair.Create(new DateTimeOffset(o.Date, TimeSpan.Zero), val));
            }
        }

        return result;
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
