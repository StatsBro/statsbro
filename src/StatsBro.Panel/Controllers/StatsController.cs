namespace StatsBro.Panel.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StatsBro.Panel.Extensions;
using StatsBro.Panel.Logic;


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
    public async Task<IActionResult> ViewAsync(Guid siteId, string query, CancellationToken cancellationToken)
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
