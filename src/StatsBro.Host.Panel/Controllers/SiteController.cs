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
using Microsoft.Extensions.Localization;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Host.Panel.Extensions;
using StatsBro.Host.Panel.Logic;
using StatsBro.Host.Panel.Models.Forms;
using System.Text;


[Authorize]
public class SiteController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SiteLogic _siteLogic;
    private readonly IStringLocalizer<HomeController> _localizer;

    public SiteController(ILogger<HomeController> logger, SiteLogic siteLogic, IStringLocalizer<HomeController> localizer)
    {
        _logger = logger;
        _siteLogic = siteLogic;
        _localizer = localizer;
    }

    public async Task<IActionResult> IndexAsync()
    {
        var sites = await _siteLogic.GetSitesAsync(HttpContext.User.GetUserId());

        return View(sites);
    }

    [HttpGet("Site/Script/{siteId}")]
    public async Task<IActionResult> ScriptAsync(Guid siteId)
    {
        var site = await _siteLogic.GetSiteAsync(HttpContext.User.GetUserId(), siteId);
        if (site == null)
        {
            return NotFound();
        }

        return View(site);
    }

    [HttpGet("Site/Settings/{siteId}")]
    public async Task<IActionResult> SettingsAsync(Guid siteId)
    {
        var site = await _siteLogic.GetSiteAsync(HttpContext.User.GetUserId(), siteId);
        if (site == null)
        {
            return NotFound();
        }

        var model = new SiteSettingsFormModel
        {
            Id = site.Id,
            SiteUrl = $"https://{site.Domain}",
            Domain = site.Domain,
            IgnoreIPsList = string.Join(Consts.SiteSettingsIPsSeparator, site.IgnoreIPsList),
            PersistQueryParamsList = string.Join(Consts.SiteSettingsQueryParamsSeparator, site.PersistQueryParamsList)
        };

        return View(model);
    }

    [HttpPost("Site/Settings/{siteId}")]
    public async Task<IActionResult> SettingsAsync(SiteSettingsFormModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _siteLogic.SaveSiteAsync(HttpContext.User.GetUserId(), model);
            }
            catch (ValidationException exc)
            {
                ModelState.AddModelError(exc.Property, exc.Message);
                model.Errors.Add(_localizer["Validation error"]);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error when trying to save settings");
                model.Errors.Add("Wystąpił nieznany błąd. Skontaktuj się z nami.");
                return View(model);
            }

            return RedirectToAction("Index", "Site");
        }
        else
        {
            model.LoadErrors(ModelState);
            return View(model);
        }
    }

    [HttpGet("Site/DataExport/{siteId}")]
    public async Task<IActionResult> DataExportAsync(Guid siteId)
    {
        var site = await _siteLogic.GetSiteAsync(HttpContext.User.GetUserId(), siteId);
        
        return View(site);
    }

    [HttpGet("Site/DataExportFile/{siteId}")]
    public async Task<IActionResult> DataExportFileAsync(Guid siteId, DateTimeOffset from, DateTimeOffset to)
    {
        string result = await _siteLogic.GetRawDataAsync(HttpContext.User.GetUserId(), siteId, from, to);
        
        return File(Encoding.UTF8.GetBytes(result), "text/csv", "statsbro_export.csv");
    }
}
