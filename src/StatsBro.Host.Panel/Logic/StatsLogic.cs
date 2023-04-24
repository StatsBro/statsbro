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
﻿namespace StatsBro.Host.Panel.Logic;

using StatsBro.Domain.Models;
using StatsBro.Domain.Models.Request;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Data;

public class StatsLogic
{
    private readonly IDbRepository _dbRepository;
    private readonly IEsStatsRepository _repositoryStats;
    private readonly ILogger<StatsLogic> _logger;

    public StatsLogic(IDbRepository dbRepository, IEsStatsRepository esStatsRepository, ILogger<StatsLogic> logger)
    {
        this._dbRepository = dbRepository;
        this._repositoryStats = esStatsRepository;
        this._logger = logger;
    }

    public async Task<StatsData> GetStatsAsync(Site site, StatsRequest request, CancellationToken cancellationToken)
    {
        var (totalUsersCount, totalEngagedUsersCount, totalAvgTimeOnSiteMilis) = await _repositoryStats.TotalUsersStatsAsync(site.Domain, request, cancellationToken);
        var generalStats = await _repositoryStats.GeneralStatsAsync(site.Domain, request, cancellationToken);
        var topStats = await _repositoryStats.TopStatsAsync(site.Domain, request, cancellationToken);
        var histograms = await _repositoryStats.HistogramStatsAsync(site.Domain, request, cancellationToken);
        var avgTimeSpent = TimeSpan.FromMilliseconds(totalAvgTimeOnSiteMilis);

        var stats = new StatsData
        {
            Summary = new StatsSummary
            {
                TotalUsersCount = totalUsersCount,
                TotalUsersChange = null,
                TotalEngagedUsersCount = totalEngagedUsersCount,
                TotalEngagedUsersChange = null,
                TotalPageViews = generalStats.EventNames.Where(e => e.Key == "pageview").Sum(s => s.Value),
                TotalPageViewChange = null,
                TotalCustomEvents = generalStats.EventNames.Where(e => e.Key != "pageview").Sum(s => s.Value),
                AverageVisitLength = new TimeSpan(avgTimeSpent.Days, avgTimeSpent.Hours, avgTimeSpent.Minutes, avgTimeSpent.Seconds), // as we want seconds precision
                AverageVisitLengthChange = null
            },
            UsersOverTime = histograms.UsersOverTime,
            EngagedUsersOverTime = histograms.EngagedUsersOverTime,
            PageViewsOverTime = histograms.ViewsOverTime,
            AverageTimeOnSiteOverTime= histograms.AvgTimeOnSiteOverTime,
            TrafficSource = generalStats.Referrers,
            Browsers = generalStats.Browsers,
            OSs = generalStats.Oses,
            TouchScreens = generalStats.IsTouchScreen,
            ScreenSizes = generalStats.ScreenSize,
            Campaigns = topStats.Campaigns,
            EntryPages = topStats.EntryPages,
            Languages = topStats.Languages,
            PagesWithMostEngagement = topStats.Engaging,
            PageViews = topStats.Pages,
            Countries = topStats.Countries,
            Cities = topStats.Cities,
            EventNames = generalStats.EventNames,
        };

        return stats;
    }


    //private LineChartViewModel GetRandomTimeSeries(params string[] seriesNames)
    //{
    //    var result = new LineChartViewModel()
    //    {
    //        Series = new List<TimeSeriesViewModel>()
    //    };

    //    foreach (var seriesName in seriesNames)
    //    {
    //        var vals = new List<TimeSeriesValuePoint>();
    //        for (int i = -30; i < 0; i++)
    //        {
    //            vals.Add(new TimeSeriesValuePoint() { TimePoint = DateTimeOffset.UtcNow.Date.AddDays(i), Value = Random.Shared.Next(50, 100) });
    //        }

    //        result.Series.Add(new TimeSeriesViewModel() { Name = seriesName, Values = vals });
    //    }

    //    return result;
    //}
}
