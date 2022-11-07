namespace StatsBro.Panel.Controllers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Panel.Extensions;
using StatsBro.Panel.Logic;
using StatsBro.Panel.Models;
using StatsBro.Panel.Models.Forms;
using System.Diagnostics;


// TODO: localization tutorial: https://www.yogihosting.com/globalization-localization-resource-files-aspnet-core/

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserLogic _userLogic;
    private readonly IStringLocalizer<HomeController> _localizer;

    public HomeController(ILogger<HomeController> logger, UserLogic userLogic, IStringLocalizer<HomeController> localizer)
    {
        _logger = logger;
        _userLogic = userLogic;
        _localizer = localizer;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Site");
    }

    [HttpGet("register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAsync(RegisterFormModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var siteId = await _userLogic.RegisterAsync(model);
                return RedirectToAction("Script", "Site", new { siteId = siteId });
            }
            catch(UserAlreadyExistsException)
            {
                model.Errors.Add(_localizer["Registration user exists"]);
                return View(model);
            }
            catch (ValidationException exc)
            {
                ModelState.AddModelError(exc.Property, exc.Message);
                model.Errors.Add(_localizer["Validation error"]);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error when trying to register user");
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

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync(LoginFormModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _userLogic.LoginAsync(model);
                return RedirectToAction("Index", "Site");
            }
            catch(InvalidCredentialsException)
            {
                model.Errors.Add(_localizer["Wrong email or password"]);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error when trying to login user");
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

    [HttpGet("logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Home");
    }

    [HttpGet("profile")]
    public async Task<IActionResult> ProfileAsync()
    {
        var userId = this.HttpContext.User.GetUserId();
        var user = await _userLogic.GetAsync(userId);

        if (user == null)
        {
            throw new EntityNotFoundException("User not found");
        }

        var model = new UserEditFormModel
        {
            Id = user.Id,
            Email = user.Email,
        };

        return View(model);
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProfileAsync(UserEditFormModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var user = await _userLogic.UpdateAsync(HttpContext.User.GetUserId(), model);
                model.Email = user.Email;
                model.Password = "";
            }
            catch (UserAlreadyExistsException)
            {
                model.Errors.Add(_localizer["User with email already exists"]);
                return View(model);
            }
            catch(ValidationException exc)
            {
                ModelState.AddModelError(exc.Property, exc.Message);
                model.Errors.Add(_localizer["Validation error"]);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error when trying to register user");
                model.Errors.Add(_localizer["Unknown error"]);
                return View(model);
            }

            return RedirectToAction("Profile", "Home");
        }
        else
        {
            model.LoadErrors(ModelState);
            return View(model);
        }
    }

    public IActionResult Help()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
