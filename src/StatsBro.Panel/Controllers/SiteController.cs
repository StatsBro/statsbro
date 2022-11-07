namespace StatsBro.Panel.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Panel.Extensions;
using StatsBro.Panel.Logic;
using StatsBro.Panel.Models.Forms;
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

    public async Task<IActionResult> ScriptAsync(Guid siteId)
    {
        var site = await _siteLogic.GetSiteAsync(HttpContext.User.GetUserId(), siteId);

        return View(site);
    }

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
            IgnoreIPsList = string.Join(", ", site.IgnoreIPsList),
            PersistQueryParamsList = string.Join(", ", site.PersistQueryParamsList)
        };

        return View(model);
    }

    [HttpPost]
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

    public async Task<IActionResult> DataExportAsync(Guid siteId)
    {
        var site = await _siteLogic.GetSiteAsync(HttpContext.User.GetUserId(), siteId);
        
        return View(site);
    }

    public async Task<IActionResult> DataExportFileAsync(Guid siteId, DateTime from, DateTime to)
    {
        await Task.Delay(500);
        string result = "id;data\r\nadsadasd;somedata";
        return File(Encoding.UTF8.GetBytes(result), "text/csv", "statsbro_export.csv");
    }
}
