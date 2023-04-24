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
using StatsBro.Host.Panel.Models.Forms.Settings;
using System.Text;


[Authorize]
public class SiteController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SiteLogic _siteLogic;
    private readonly OrganizationLogic _organizationLogic;
    private readonly IStringLocalizer<HomeController> _localizer;
    private readonly ISubscriptionPlanGuard _subscriptionPlanGuard;

    public SiteController(
        ILogger<HomeController> logger,
        SiteLogic siteLogic,
        OrganizationLogic organizationLogic,
        IStringLocalizer<HomeController> localizer,
        ISubscriptionPlanGuard subscriptionPlanGuard)
    {
        _logger = logger;
        _siteLogic = siteLogic;
        _organizationLogic = organizationLogic;
        _localizer = localizer;
        _subscriptionPlanGuard = subscriptionPlanGuard;
    }

    public async Task<IActionResult> IndexAsync()
    {
        try
        {
            var organizationId = User.GetOrganizationId();
            var organization = await _organizationLogic.GetOrganizationAsync(organizationId);

            var userId = User.GetUserId();
            var sites = await _siteLogic.GetSitesAsync(userId, organizationId);

            if (!_subscriptionPlanGuard.IsSubscriptionPlanExpired(organization))
            {
                return RedirectToAction("Index", "Payment");
            }
            ViewBag.CanAddMoreDomains = false;
            if (User.IsInRole(StatsBro.Domain.Models.DTO.OrganizationUserRole.Admin.ToString()))
            {
                ViewBag.CanAddMoreDomains = _subscriptionPlanGuard.CanAddMoreDomains(organization, sites.Count);
            }

            return View(sites);
        }
        catch (EntityNotFoundException ex)
        {
            this._logger.LogError(ex, "There is something wrong with data integrity in session");
            this._logger.LogError("redirecting user to logout");
            
            return RedirectToAction("Logout", "Home");
        }
    }

    [HttpGet("Site/Script/{siteId}")]
    public async Task<IActionResult> ScriptAsync(Guid siteId)
    {
        var userId = HttpContext.User.GetUserId();
        var organizationId = HttpContext.User.GetOrganizationId();
        var site = await _siteLogic.GetSiteAsync(userId, organizationId, siteId);
        if (site == null)
        {
            return NotFound();
        }

        return View(site);
    }

    [HttpGet("Site/Settings/{siteId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SettingsAsync(Guid siteId)
    {
        var model = await GetSettingsFormModel(siteId);
        if (model == null)
        {
            return NotFound();
        }

        var organizationId = User.GetOrganizationId();
        var organization = await _organizationLogic.GetOrganizationAsync(organizationId);
        if (!_subscriptionPlanGuard.IsSubscriptionPlanExpired(organization))
        {
            return RedirectToAction("Index", "Payment");
        }

        return View(model);
    }

    private async Task<SiteSettingsFormModel?> GetSettingsFormModel(Guid siteId)
    {
        var userId = HttpContext.User.GetUserId();
        var organizationId = HttpContext.User.GetOrganizationId();
        var site = await _siteLogic.GetSiteAsync(userId, organizationId, siteId);
        if (site == null)
        {
            return null;
        }
        
        var sharingSettings = await _siteLogic.GetSiteSharingSettingsAsync(siteId, Url);
        var model = new SiteSettingsFormModel
        {
            General = new GeneralSettingsFormModel
            {
                Id = site.Id,
                SiteUrl = $"https://{site.Domain}",
                Domain = site.Domain,
                IgnoreIPsList = string.Join(Consts.SiteSettingsIPsSeparator, site.IgnoreIPsList),
                PersistQueryParamsList = string.Join(Consts.SiteSettingsQueryParamsSeparator, site.PersistQueryParamsList)
            },
            Sharing = sharingSettings
        };

        return model;
    }

    [HttpPost("Site/SettingsGeneral")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveSettingsGeneralAsync(GeneralSettingsFormModel model)
    {
        var settingsModel = await GetSettingsFormModel(model.Id);
        if (settingsModel == null)
        {
            return NotFound();
        }

        settingsModel.General = model;
        if (ModelState.IsValid)
        {
            try
            {
                var userId = User.GetUserId();
                var organizationId = User.GetOrganizationId();
                await _siteLogic.SaveSiteAsync(userId, organizationId, model);
            }
            catch (ValidationException exc)
            {
                ModelState.AddModelError(exc.Property, exc.Message);
                model.Errors.Add(_localizer["Validation error"]);
                return View("Settings", settingsModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error when trying to save settings");
                model.Errors.Add("Wystąpił nieznany błąd. Skontaktuj się z nami.");
                return View("Settings", settingsModel);
            }

            return RedirectToAction("Settings", "Site", new { siteId = model.Id });
        }
        else
        {
            model.LoadErrors(ModelState);
            return View("Settings", settingsModel);
        }
    }

    [HttpPost("Site/SettingsSharing")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveSettingsSharingAsync(SharingSettingsFormModel model)
    {
        var settingsModel = await GetSettingsFormModel(model.Id);
        if (settingsModel == null)
        {
            return NotFound();
        }

        settingsModel.Sharing = model;
        if (ModelState.IsValid)
        {
            try
            {
                await _siteLogic.SaveSiteSharingSettingsAsync(model);
                await _siteLogic.SaveSiteApiSettingsAsync(model);
            }
            catch (ValidationException exc)
            {
                ModelState.AddModelError(exc.Property, exc.Message);
                model.Errors.Add(_localizer["Validation error"]);
                return View("Settings", settingsModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error when trying to save settings");
                model.Errors.Add("Wystąpił nieznany błąd. Skontaktuj się z nami.");
                return View("Settings", settingsModel);
            }

            return RedirectToAction("Settings", "Site", new { siteId = model.Id }, "sharing-tab");
        }
        else
        {
            model.LoadErrors(ModelState);
            return View("Settings", settingsModel);
        }
    }

    [HttpGet("Site/DataExport/{siteId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DataExportAsync(Guid siteId)
    {
        var userId = HttpContext.User.GetUserId();
        var organizationId = HttpContext.User.GetOrganizationId();

        var organization = await _organizationLogic.GetOrganizationAsync(organizationId);
        if (!_subscriptionPlanGuard.IsSubscriptionPlanExpired(organization))
        {
            return RedirectToAction("Index", "Payment");
        }

        var site = await _siteLogic.GetSiteAsync(userId, organizationId, siteId);
        
        return View(site);
    }

    [HttpGet("Site/DataExportFile/{siteId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DataExportFileAsync(Guid siteId, DateTimeOffset from, DateTimeOffset to)
    {
        var userId = HttpContext.User.GetUserId();
        var organizationId = HttpContext.User.GetOrganizationId();
        string result = await _siteLogic.GetRawDataFileAsync(userId, organizationId, siteId, from, to);
        
        return File(Encoding.UTF8.GetBytes(result), "text/csv", "statsbro_export.csv");
    }

    [HttpGet("Site/New")]
    [Authorize(Roles = "Admin")]
    public IActionResult NewAsync()
    {
        return View();
    }

    [HttpPost("Site/New")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> NewAsync(AddSiteFormModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var orgId = User.GetOrganizationId();
                var canAddMoreDomains = await _subscriptionPlanGuard.CanAddMoreDomainsAsync(orgId);

                if (!canAddMoreDomains)
                {
                    _logger.LogWarning("For {organizationId} there was a try to add new domain/site but subscription plan limit was reached.", orgId);
                    return View(model);
                }

                var userId = User.GetUserId();
                var site = await _siteLogic.AddNewSiteAsync(userId, orgId, model.SiteUrl);
                return RedirectToAction("Script", "Site", new { siteId = site.Id });
            }
            catch (ValidationException exc)
            {
                ModelState.AddModelError(exc.Property, exc.Message);
                model.Errors.Add(_localizer["Validation error"]);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error when trying to add new site");
                model.Errors.Add(_localizer["Unknown error"]);
                return View(model);
            }
        }
        else
        {
            model.LoadErrors(ModelState);
            return View(model);
        }
    }
}
