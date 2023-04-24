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
using StatsBro.Domain.Models.Request;
using StatsBro.Domain.Models.Stats;
using System.Threading.Tasks;



public interface IEsStatsRepository
{
    Task<(long, long, long)> TotalUsersStatsAsync(string domain, StatsRequest request, CancellationToken cancellationToken);
    Task<HistogramStatsResult> HistogramStatsAsync(string domain, StatsRequest request, CancellationToken cancellationToken = default);
    Task<GeneralStatsResult> GeneralStatsAsync(string domain, StatsRequest request, CancellationToken cancellationToken = default);
    Task<TopStatsResult> TopStatsAsync(string domain, StatsRequest request, CancellationToken cancellationToken = default);
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

    public async Task<(long, long, long)> TotalUsersStatsAsync(string domain, StatsRequest request, CancellationToken cancellationToken = default)
    {
        var aggCardinalityName = "aggc1";
        var aggFilterEngName = "agg_eng";
        var aggCardinalityEngName = "aggc2";
        var aggAverageTimeOnSite = "agg_avgtime";

        var result = await this.Client.SearchAsync<SiteVisitData>(s =>
            s.Index(Indexing.IndexName(this._esConfig, domain))
            .Size(0)
            .BuildQueryFromRequest(request, GetTimeZone())
            .Aggregations(a => a
                .Cardinality(aggCardinalityName, c => c.Field(f => f.Hash))
                .Filter(aggFilterEngName, at => at.Filter(q => q.Term(t => t.Field("referrer.domain").Value(domain)))
                    .Aggregations(a => a.Cardinality(aggCardinalityEngName, c => c.Field(f => f.Hash)))
                    )
                .Average(aggAverageTimeOnSite, c => c.Field(f => f.TimeSpentMillis))
            ),
            cancellationToken
        );

        var totalCount = (long)(result.Aggregations.Cardinality(aggCardinalityName)?.Value ?? 0);
        var totalEngagedCount = (long)(result.Aggregations.Filter(aggFilterEngName)?.Cardinality(aggCardinalityEngName)?.Value ?? 0);
        var totalAvgTimeOnSite = (long)(result.Aggregations.Average(aggAverageTimeOnSite)?.Value ?? 0);

        return (totalCount, totalEngagedCount, totalAvgTimeOnSite);
    }

    public async Task<GeneralStatsResult> GeneralStatsAsync(string domain, StatsRequest request, CancellationToken cancellationToken = default)
    {
        var aggNameSceenSize = "screensize";
        var aggNameBrowser = "browser";
        var aggNameOs = "os";
        var aggNameIsTouchScreen = "touchscreen";
        var aggNameReferrerDomain = "refferer";
        var aggNameEventName = "event";

        var result = await this.Client.SearchAsync<dynamic>(s =>
            s.Index(Indexing.IndexName(this._esConfig, domain))
            .Size(0)
            .BuildQueryFromRequest(request, GetTimeZone())
            .Aggregations(a => 
                a
                .Terms(aggNameReferrerDomain, at => at.Field("referrer.domain.keyword").Exclude(domain).Missing("direct").Size(10))
                .Terms(aggNameOs, at => at.Field("user_agent.os.full.keyword").Size(10))
                .Terms(aggNameIsTouchScreen, at => at.Field("is_touch_screen").Size(2))
                .Terms(aggNameSceenSize, at => at.Field("screen_size.keyword").Size(10))
                .Terms(aggNameBrowser, at => at.Field("user_agent.name.keyword").Size(10))
                .Terms(aggNameEventName, at => at.Field("event").Size(1000))
                ),
            cancellationToken);

        var reffererDomainResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameReferrerDomain));
        var osesResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameOs));
        var browsersResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameBrowser));
        var screenSizeResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameSceenSize));
        var isTouchScreenResult = UnpackTermsBucket(result.Aggregations.Terms(aggNameIsTouchScreen), new Dictionary<string, string> { { "1", "Yes" }, { "0", "No" } });
        var eventNames = UnpackTermsBucket(result.Aggregations.Terms(aggNameEventName));

        return new GeneralStatsResult 
        {
            Referrers = reffererDomainResult,
            Oses = osesResult,
            Browsers = browsersResult,
            IsTouchScreen = isTouchScreenResult,
            ScreenSize = screenSizeResult,
            EventNames = eventNames,
        };
    }

    public async Task<TopStatsResult> TopStatsAsync(string domain, StatsRequest request, CancellationToken cancellationToken = default)
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
           .BuildQueryFromRequest(request, GetTimeZone())
           .Aggregations(a =>
               a.Terms(aggNamePages, at => at.Field("url.path.keyword").Size(1000))
                .Terms(aggNameCountries, at => at.Field("geo.country_name.keyword").Size(1000))
                .Terms(aggNameCities, at => at.Field("geo.city_name.keyword").Size(1000))
                .Terms(aggNameLang, at => at.Field("lang").Size(1000))
                .Terms(aggNamePopularCampaigns, at => at.Field("url.query_params.utm_campaign").Size(1000))
                // to get entry pages we first filter docs to have referrer different than own domain
                .Filter(aggNameEntryPagesFilter, at => at.Filter(q => 
                    q.Bool(b => 
                        b.Should(qs => qs.MatchAll()).MustNot(qs => qs.Term(t => t.Field("referrer.domain").Value(domain)))
                        )
                    )
                    .Aggregations(a => a.Terms(aggNamePages, at => at.Field("url.path.keyword").Size(1000)))
                    )
                // to get most engaging pages we get top reffering within site domain
                .Filter(aggNameMostEngagingPages, at => at.Filter(q => q.Term(t => t.Field("referrer.domain").Value(domain)))
                    .Aggregations(a => a.Terms(aggNamePages, at => at.Field("refferer.url.path").Size(1000)))
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

    public async Task<HistogramStatsResult> HistogramStatsAsync(string domain, StatsRequest request, CancellationToken cancellationToken = default)
    {
        var aggNameViewsOverTime = "views_over_time";
        var aggNameUsersOverTime = "users_over_time";
        var aggNameSubUsersOverTimeCardinailty = "sub_users_over_time";
        var aggNameFilterEngaged = "filter_engaged";
        var aggNameFilterPageviews = "filter_pageviews";
        var aggNameAvgTimeOnSiteOverTime = "avgtime_over_time";
        var aggNameSubAvgTimeOnSiteOverTime = "sub_avgtime_over_time";
        var interval = GetHistogramInterval(request);

        var result = await this.Client.SearchAsync<dynamic>(s =>
           s.Index(Indexing.IndexName(this._esConfig, domain))
           .Size(0)
           .BuildQueryFromRequest(request, GetTimeZone())
           .Aggregations(a =>
               a
               .Filter(aggNameFilterPageviews, at => at.Filter(q => q.Term(t => t.Field("event").Value("pageview")))
                    .Aggregations(a => a.DateHistogram(aggNameViewsOverTime, at => at.Field("@timestamp").CalendarInterval(interval).TimeZone(GetTimeZone())))
               )     
               .DateHistogram(aggNameAvgTimeOnSiteOverTime, at =>
                    at.Field("@timestamp").CalendarInterval(interval).TimeZone(GetTimeZone())
                        .Aggregations(suba =>
                            suba.Average(aggNameSubAvgTimeOnSiteOverTime, at => at.Field("time_spent"))
                            )
                        )
               .DateHistogram(aggNameUsersOverTime, at => 
                    at.Field("@timestamp").CalendarInterval(interval).TimeZone(GetTimeZone())
                        .Aggregations(suba => 
                            suba.Cardinality(aggNameSubUsersOverTimeCardinailty, at => at.Field("hash"))
                            )
                        )
               .Filter(aggNameFilterEngaged, at => at.Filter(q => q.Term(t => t.Field("referrer.domain").Value(domain)))
                    .Aggregations(a => a.DateHistogram(aggNameUsersOverTime, at =>
                    at.Field("@timestamp").CalendarInterval(interval).TimeZone(GetTimeZone())
                        .Aggregations(suba =>
                            suba.Cardinality(aggNameSubUsersOverTimeCardinailty, at => at.Field("hash"))
                            )
                        )
                    )
               )
                ),
           cancellationToken);

        var viewsOverTime = UnpackHistogramBucket(request, interval, result.Aggregations.Filter(aggNameFilterPageviews)?.DateHistogram(aggNameViewsOverTime));
        var usersOverTime = UnpackHistogramBucket(request, interval, result.Aggregations.DateHistogram(aggNameUsersOverTime), aggNameSubUsersOverTimeCardinailty);
        var avgVisitTimeOverTime = UnpackHistogramBucket(request, interval, result.Aggregations.DateHistogram(aggNameAvgTimeOnSiteOverTime), aggNameSubAvgTimeOnSiteOverTime);
        var engagedOverTime = UnpackHistogramBucket(request, interval, result.Aggregations.Filter(aggNameFilterEngaged)?.DateHistogram(aggNameUsersOverTime), aggNameSubUsersOverTimeCardinailty);

        return new HistogramStatsResult
        {
            ViewsOverTime = viewsOverTime,
            UsersOverTime = usersOverTime,
            EngagedUsersOverTime = engagedOverTime,
            AvgTimeOnSiteOverTime = avgVisitTimeOverTime
        };
    }

    private DateInterval GetHistogramInterval(StatsRequest request)
    {
        var days = (request.To - request.From)!.Value.TotalDays;

        if (days <= 3) return DateInterval.Hour;
        
        return DateInterval.Day;
    }

    private IList<KeyValuePair<DateTimeOffset, long>> UnpackHistogramBucket(StatsRequest request, DateInterval interval, MultiBucketAggregate<DateHistogramBucket>? agg, string? subKey = null)
    {
        var result = new List<KeyValuePair<DateTimeOffset, long>>();
        var fromBucket = new Dictionary<DateTimeOffset, long>();

        if (interval != DateInterval.Hour && interval != DateInterval.Day && interval != DateInterval.Month)
        {
            throw new Exception("Unsupported interval");
        }

        // unpack the result from ES
        if (agg != null)
        {
            foreach (var o in agg.Buckets)
            {
                var val = subKey == null ? (long)o.DocCount! : (long)(o.ValueCount(subKey).Value ?? 0L);
                // can be constructed just from Date because o.Date is of UTC kind
                fromBucket[new DateTimeOffset(o.Date)] = val;
            }
        }

        //points in time with 0 documents will be missing so we need add them
        var date = new DateTimeOffset(request.From!.Value, GetUTCOffset());
        var end = new DateTimeOffset(request.To!.Value, GetUTCOffset());
        
        while (date <= end)
        {
            if (fromBucket.ContainsKey(date))
            {
                result.Add(new KeyValuePair<DateTimeOffset, long>(date, fromBucket[date]));
            }
            else
            {
                result.Add(new KeyValuePair<DateTimeOffset, long>(date, 0));
            }

            if (interval == DateInterval.Hour) date = date.AddHours(1);
            if (interval == DateInterval.Day) date = date.AddDays(1);
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

    private TimeSpan GetUTCOffset()
    {
        return DateTimeOffset.Now.Offset;
    }

    private string GetTimeZone()
    {
        return "+" + GetUTCOffset().ToString(@"hh\:mm");
    }

    private IElasticClient Client
    {
        get
        {
            return _esFactory.GetClient();
        }
    }
}
