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
using StatsBro.Host.Panel.Models;
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

    public async Task<StatsViewModel> GetStatsAsync(Site site, string query, CancellationToken cancellationToken)
    {
        var (totalUsersCount, totalEngagedUsersCount) = await _repositoryStats.TotalUsersCountAsync(site.Domain, query, cancellationToken);
        var totalPageViews = await _repositoryStats.TotalPageviewsCountAsync(site.Domain, query, cancellationToken);
        var generalStats = await _repositoryStats.GeneralStatsAsync(site.Domain, query, cancellationToken);
        var topStats = await _repositoryStats.TopStatsAsync(site.Domain, query, cancellationToken);
        var histograms = await _repositoryStats.HistogramStatsAsync(site.Domain, query, cancellationToken);

        var usersChart = new LineChartViewModel
        {
            Series = new List<TimeSeriesViewModel>()
        };

        usersChart.Series.Add(GetTimeSeries(histograms.UsersOverTime, "Użytkownicy"));
        usersChart.Series.Add(GetTimeSeries(histograms.EngagedUsersOverTime, "Zaangażowani użytkownicy"));

        var pageViewsChart = new LineChartViewModel
        {
            Series = new List<TimeSeriesViewModel>()
        };

        pageViewsChart.Series.Add(GetTimeSeries(histograms.ViewsOverTime, "Odsłony"));

        // TODO: time range: 
        var stats = new StatsViewModel()
        {
            Query = query,
            Summary = new StatsSummaryViewModel
            {
                TotalUsersCount = totalUsersCount,
                TotalUsersChange = null,
                TotalEngagedUsersCount = totalEngagedUsersCount,
                TotalEngagedUsersChange = null,
                TotalPageViews = totalPageViews,
                TotalPageViewChange = null,
                AverageVisitLength = new TimeSpan(0, 0, 0),
                AverageVisitLengthChange = null
            },
            UsersInTime = usersChart,
            PageViewsInTime = pageViewsChart,
            AverageVisitLengthInTime = GetRandomTimeSeries("Czas wizyty"),
            TrafficSources = this.GetPieChart(generalStats.Referrers),
            Browsers = this.GetPieChart(generalStats.Browsers),
            OSs = this.GetPieChart(generalStats.Oses),
            TouchScreens = this.GetPieChart(generalStats.IsTouchScreen),
            ScreenSizes = this.GetPieChart(generalStats.ScreenSize),
            Campaigns = GetTable(topStats.Campaigns, "url.query_params.utm_campaign", query),
            EntryPages = GetTable(topStats.EntryPages, "url.path", query),
            Languages = GetTable(topStats.Languages, "lang", query),
            PagesWithMostEngagement = GetTable(topStats.Engaging, "url.path", query),
            PageViews = GetTable(topStats.Pages, "url.path", query),
            Countries = GetTable(topStats.Countries, "geo.country_name", query),
            Cities = GetTable(topStats.Cities, "geo.city_name", query),
            Site = site
        };

        return stats;
    }

    private TimeSeriesViewModel GetTimeSeries(IList<KeyValuePair<DateTimeOffset, long>> viewsOverTime, string name)
    {
        return
            new TimeSeriesViewModel
            {
                Name = name,
                Values = viewsOverTime.Select(kv => new TimeSeriesValuePoint { TimePoint = kv.Key, Value = kv.Value }).ToList(),
            };
    }

    private TableViewModel GetRandomTable(string fieldName, string query)
    {
        var data = new DataTable();
        data.Columns.Add("Nazwa");
        data.Columns.Add("Wyświetlenia");

        var start = Random.Shared.Next(1000, 10000);
        for (int i = 0; i < 10; i++)
        {
            data.Rows.Add($"nazwa_{i}", start--);
        }

        return new TableViewModel { Data = data, FieldName = fieldName, ExistingQuery = query };
    }

    private TableViewModel GetTable(IList<KeyValuePair<string, long>> payload, string fieldName, string query)
    {
        var data = new DataTable();
        data.Columns.Add("Nazwa"); // TODO: translate this????  or maybe this should be translated in UI?
        data.Columns.Add("Wyświetlenia");

        foreach (var kvp in payload)
        {
            data.Rows.Add(kvp.Key, kvp.Value);
        }

        return new TableViewModel { Data = data, FieldName = fieldName, ExistingQuery = query };
    }

    private PieChartViewModel GetPieChart(IList<KeyValuePair<string, long>> items)
    {
        var result = new PieChartViewModel
        {
            Items = new List<PieChartItem>()
        };

        var total = items.Sum(x => x.Value);
        foreach (var item in items)
        {
            int share = (int)(total / item.Value);
            //result.Items.Add(new PieChartItem { Name = item.Key, Value = share });
            result.Items.Add(new PieChartItem { Name = item.Key, Value = item.Value});
        }

        return result;
    }

    private LineChartViewModel GetRandomTimeSeries(params string[] seriesNames)
    {
        var result = new LineChartViewModel()
        {
            Series = new List<TimeSeriesViewModel>()
        };

        foreach (var seriesName in seriesNames)
        {
            var vals = new List<TimeSeriesValuePoint>();
            for (int i = -30; i < 0; i++)
            {
                vals.Add(new TimeSeriesValuePoint() { TimePoint = DateTimeOffset.UtcNow.Date.AddDays(i), Value = Random.Shared.Next(50, 100) });
            }

            result.Series.Add(new TimeSeriesViewModel() { Name = seriesName, Values = vals });
        }

        return result;
    }
}
