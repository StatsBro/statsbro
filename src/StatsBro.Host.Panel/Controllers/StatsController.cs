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
using StatsBro.Host.Panel.Extensions;
using StatsBro.Host.Panel.Logic;


[Authorize]
public class StatsController : Controller
{
    private readonly ILogger<StatsController> _logger;
    private readonly StatsLogic _statsLogic;
    private readonly SiteLogic _siteLogic;

    public StatsController(StatsLogic statsLogic, SiteLogic siteLogic, ILogger<StatsController> logger)
    {
        _logger = logger;
        _statsLogic = statsLogic;
        _siteLogic = siteLogic;
    }

    [HttpGet("Stats/View/{siteId}")]
    public async Task<IActionResult> ViewAsync([FromRoute]Guid siteId, string query, CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.GetUserId();
        var site = await this._siteLogic.GetSiteAsync(userId, siteId);
        if (site == null)
        {
            return NotFound();
        }

        var stats = await _statsLogic.GetStatsAsync(site, query, cancellationToken);

        return View(stats);
    }
}
