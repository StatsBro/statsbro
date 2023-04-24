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
﻿namespace StatsBro.Host.Panel.Controllers.Api.V1;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StatsBro.Host.Panel.Controllers.API.V1.Models;
using StatsBro.Host.Panel.Extensions;
using StatsBro.Host.Panel.Infrastructure.AuthHandlers.HeaderToken;
using StatsBro.Host.Panel.Logic;

[ApiExplorerSettings(GroupName = "v1")]
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = HeaderTokenAuthHandler.AuthSchemaName)]
public class SiteStatsController : Controller
{
    private readonly StatsLogic _statsLogic;
    private readonly SiteLogic _siteLogic;
    private readonly ILogger<SiteStatsController> _logger;

    public SiteStatsController(
        StatsLogic statsLogic,
        SiteLogic siteLogic,
        ILogger<SiteStatsController> logger)
    {
        _statsLogic = statsLogic;
        _siteLogic = siteLogic;
        _logger = logger;
    }

    
    /// <summary>
    /// Returns Site Statistics.
    /// Response contains various statistics collected in the service.
    /// </summary>
    [HttpGet()]
    [ProducesResponseType(typeof(SiteStatsData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SiteStatsAsync(        
        [FromQuery] SiteStatsQuery request,
        CancellationToken cancellationToken)
    {
        var siteId = HttpContext.User.GetSiteId();
        var site = await this._siteLogic.GetSiteAsync(siteId);
        if (site == null)
        {
            return NotFound();
        }

        var stats = await _statsLogic.GetStatsAsync(site, request.ToStatsRequest(), cancellationToken);
        var result = new SiteStatsData
        {
            Summary = new SiteStatsSummary
            {
                TotalUsersCount = stats.Summary.TotalUsersCount,
                TotalUsersChange = null,
                TotalEngagedUsersCount = stats.Summary.TotalEngagedUsersCount,
                TotalEngagedUsersChange = null,
                TotalPageViews = stats.Summary.TotalPageViews,
                TotalPageViewChange = null,
                TotalCustomEvents = stats.Summary.TotalCustomEvents,
                AverageVisitLength = stats.Summary.AverageVisitLength,
                AverageVisitLengthChange = null,
            },
            UsersOverTime = ToValuePairTime(stats.UsersOverTime),
            EngagedUsersOverTime = ToValuePairTime(stats.EngagedUsersOverTime),
            PageViewsOverTime = ToValuePairTime(stats.PageViewsOverTime),
            AverageTimeOnSiteOverTime = ToValuePairTime(stats.AverageTimeOnSiteOverTime),
            TrafficSource = stats.TrafficSource,
            Browsers = stats.Browsers,
            OSs = stats.OSs,
            TouchScreens = stats.TouchScreens,
            ScreenSizes = stats.ScreenSizes,
            Campaigns = stats.Campaigns,
            EntryPages = stats.EntryPages,
            Languages = stats.Languages,
            PagesWithMostEngagement = stats.PagesWithMostEngagement,
            PageViews = stats.PageViews,
            Countries = stats.Countries,
            Cities = stats.Cities,
            Events = stats.EventNames,
        };

        return Ok(result);
    }

    private static IEnumerable<KeyValuePairTime> ToValuePairTime(IList<KeyValuePair<DateTimeOffset, long>> kvs)
    {
        return kvs.Select(x => new KeyValuePairTime { Time = x.Key, Value = x.Value });
    }
}
