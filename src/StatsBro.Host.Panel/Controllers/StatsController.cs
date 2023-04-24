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
﻿namespace StatsBro.Host.Panel.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StatsBro.Domain.Models;
using StatsBro.Domain.Models.Request;
using StatsBro.Host.Panel.Extensions;
using StatsBro.Host.Panel.Logic;
using StatsBro.Host.Panel.Models;
using System.Data;
using System.Web;
using static StatsBro.Host.Panel.Extensions.StatsRequestLinkBuilder;

[Authorize]
public class StatsController : Controller
{
    private readonly ILogger<StatsController> _logger;
    private readonly StatsLogic _statsLogic;
    private readonly SiteLogic _siteLogic;
    private readonly OrganizationLogic _organizationLogic;
    private readonly ISubscriptionPlanGuard _subscriptionPlanGuard;

    public StatsController(
        StatsLogic statsLogic,
        SiteLogic siteLogic,
        OrganizationLogic organizationLogic,
        ISubscriptionPlanGuard subscriptionPlanGuard,
        ILogger<StatsController> logger)
    {
        _logger = logger;
        _statsLogic = statsLogic;
        _siteLogic = siteLogic;
        _organizationLogic = organizationLogic;
        _subscriptionPlanGuard = subscriptionPlanGuard;
    }

    [HttpGet("Stats/View/{siteId}")]
    public async Task<IActionResult> ViewAsync([FromRoute]Guid siteId, [FromQuery]StatsRequest request, CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.GetUserId();
        var organizationId = HttpContext.User.GetOrganizationId();
        var organization = await _organizationLogic.GetOrganizationAsync(organizationId);
        if (!_subscriptionPlanGuard.IsSubscriptionPlanExpired(organization))
        {
            return RedirectToAction("Index", "Payment");
        }

        var site = await this._siteLogic.GetSiteAsync(userId, organizationId, siteId);
        if (site == null)
        {
            return NotFound();
        }

        request.To = request.To.Value.AddDays(1).AddSeconds(-1);

        ViewBag.Request = request;
        ViewBag.LinkBuilder = new StatsRequestLinkBuilder(request);
        var stats = await _statsLogic.GetStatsAsync(site, request, cancellationToken);
        var viewModel = this.StatsToViewModel(stats, site);

        return View(viewModel);
    }

    [HttpGet("Stats/Share/{shareId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ShareAsync([FromRoute] string shareId, [FromQuery] StatsRequest request, CancellationToken cancellationToken)
    {
        var site = await this._siteLogic.GetSiteAsync(HttpUtility.UrlDecode(shareId));
        if (site == null)
        {
            return NotFound();
        }
        
        // no access when subscription expired
        var organization = await _organizationLogic.GetOrganizationAsync(site.OrganizationId);
        if (!_subscriptionPlanGuard.IsSubscriptionPlanExpired(organization))
        {
            return NotFound();
        }

        ViewBag.Request = request;
        ViewBag.LinkBuilder = new StatsRequestLinkBuilder(request);
        var stats = await _statsLogic.GetStatsAsync(site, request, cancellationToken);

        var viewModel = this.StatsToViewModel(stats, site);
        return View(viewModel);
    }

    [HttpGet("Stats/Live/{siteId}")]
    public async Task<IActionResult> LiveAsync([FromRoute] Guid siteId, CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.GetUserId();
        var organizationId = HttpContext.User.GetOrganizationId();
        var site = await this._siteLogic.GetSiteAsync(userId, organizationId, siteId);
        if (site == null)
        {
            return NotFound();
        }

        var organization = await _organizationLogic.GetOrganizationAsync(organizationId);
        if (!_subscriptionPlanGuard.IsSubscriptionPlanExpired(organization))
        {
            return RedirectToAction("Index", "Site");
        }

        var model = new LiveViewModel
        {
            Site = site,
            UpdateUrl = Url.Action("LiveUpdate", new { siteId })
        };

        return View(model);
    }

    [HttpGet("Stats/LiveUpdate/{siteId}")]
    public async Task<IActionResult> LiveUpdateAsync([FromRoute] Guid siteId, CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.GetUserId();
        var organizationId = HttpContext.User.GetOrganizationId();
        
        var to = DateTime.Now;
        var from = DateTime.Now.AddMinutes(-15);
        var data = await _siteLogic.GetRawDataAsync(userId, organizationId, siteId, from, to);

        return View(data.ToList());
    }

    private StatsViewModel StatsToViewModel(StatsData data, Site site)
    {
        var usersChart = new LineChartViewModel
        {
            Series = new List<TimeSeriesViewModel>()
        };

        usersChart.Series.Add(GetTimeSeries(data.UsersOverTime, "Użytkownicy"));
        usersChart.Series.Add(GetTimeSeries(data.EngagedUsersOverTime, "Zaangażowani użytkownicy"));

        var pageViewsChart = new LineChartViewModel
        {
            Series = new List<TimeSeriesViewModel>()
        };

        pageViewsChart.Series.Add(GetTimeSeries(data.PageViewsOverTime, "Odsłony"));

        var timeOnSiteChart = new LineChartViewModel
        {
            Series = new List<TimeSeriesViewModel>()
        };

        timeOnSiteChart.Series.Add(GetTimeSeries(data.AverageTimeOnSiteOverTime, "time"));

        var stats = new StatsViewModel()
        {
            Summary = new StatsSummaryViewModel
            {
                TotalUsersCount = data.Summary.TotalUsersCount,
                TotalUsersChange = null,
                TotalEngagedUsersCount = data.Summary.TotalEngagedUsersCount,
                TotalEngagedUsersChange = null,
                TotalPageViews = data.Summary.TotalPageViews,
                TotalPageViewChange = null,
                TotalCustomEvents = data.Summary.TotalCustomEvents,
                AverageVisitLength = data.Summary.AverageVisitLength,
                AverageVisitLengthChange = null,
            },
            UsersInTime = usersChart,
            PageViewsInTime = pageViewsChart,
            AverageVisitLengthInTime = timeOnSiteChart,
            TrafficSources = this.GetTable(data.TrafficSource, StatsRequestFitlerType.ByReferrer),
            Browsers = this.GetPieChart(data.Browsers),
            OSs = this.GetPieChart(data.OSs),
            TouchScreens = this.GetPieChart(data.TouchScreens),
            ScreenSizes = this.GetPieChart(data.ScreenSizes),
            Campaigns = GetTable(data.Campaigns, StatsRequestFitlerType.ByUtmCampaign),
            EntryPages = GetTable(data.EntryPages, StatsRequestFitlerType.ByUrl),
            Languages = GetTable(data.Languages, StatsRequestFitlerType.ByLang),
            PagesWithMostEngagement = GetTable(data.PagesWithMostEngagement, StatsRequestFitlerType.ByUrl),
            PageViews = GetTable(data.PageViews, StatsRequestFitlerType.ByUrl),
            Countries = GetTable(data.Countries, StatsRequestFitlerType.ByCountry),
            Cities = GetTable(data.Cities, StatsRequestFitlerType.ByCity),
            EventNames = GetTable(data.EventNames, StatsRequestFitlerType.ByEventName),
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

    private TableViewModel GetTable(IList<KeyValuePair<string, long>> payload, StatsRequestFitlerType filterType)
    {
        var data = new DataTable();
        data.Columns.Add("Nazwa");
        data.Columns.Add("Zdarzenia");

        foreach (var kvp in payload)
        {
            data.Rows.Add(kvp.Key, kvp.Value);
        }

        return new TableViewModel { Data = data, FilterType = filterType };
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
            result.Items.Add(new PieChartItem { Name = item.Key, Value = item.Value });
        }

        return result;
    }
}
