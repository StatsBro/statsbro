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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Host.Panel.Extensions;
using StatsBro.Host.Panel.Logic;
using StatsBro.Host.Panel.Models;
using StatsBro.Host.Panel.Models.Forms;
using System.Diagnostics;


// TODO: localization tutorial: https://www.yogihosting.com/globalization-localization-resource-files-aspnet-core/

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserLogic _userLogic;
    private readonly PaymentLogic _paymentLogic;
    private readonly OrganizationLogic _organizationLogic;
    private readonly IStringLocalizer<HomeController> _localizer;
    private readonly ISubscriptionPlanGuard _subscriptionPlanGuard;

    public HomeController(
        ILogger<HomeController> logger,
        UserLogic userLogic,
        PaymentLogic paymentLogic,
        OrganizationLogic organizationLogic,
        IStringLocalizer<HomeController> localizer,
        ISubscriptionPlanGuard subscriptionPlanGuard)
    {
        _logger = logger;
        _userLogic = userLogic;
        _paymentLogic = paymentLogic;
        _organizationLogic = organizationLogic;
        _localizer = localizer;
        _subscriptionPlanGuard = subscriptionPlanGuard;
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
                _logger.LogError(ex, "Unknown error when trying to edit user profile");
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

    [HttpGet("usersmanagement")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UsersManagementAsync()
    {
        var users = await this.PutOrganizationUsersToViewDataAsync();
        var orgId = HttpContext.User.GetOrganizationId();
        var organization = await _organizationLogic.GetOrganizationAsync(orgId);
        
        if(!_subscriptionPlanGuard.IsSubscriptionPlanExpired(organization))
        {
            return RedirectToAction("Index", "Payment");
        }

        ViewBag.CanAddMoreUsers = _subscriptionPlanGuard.CanAddMoreUsers(organization, users.Count);

        return View(new OrganizationUserFormModel());
    }

    [HttpPost("usersmanagement")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UsersManagementAsync(OrganizationUserFormModel model)
    {
        if (!ModelState.IsValid)
        {
            await this.PutOrganizationUsersToViewDataAsync();
            model.LoadErrors(ModelState);
            return View(model);
        }

        try
        {
            var orgId = User.GetOrganizationId();
            var canAddMoreUsers = await _subscriptionPlanGuard.CanAddMoreUsersAsync(orgId);
            if (!canAddMoreUsers)
            {
                _logger.LogWarning("For {organizationId} there was a try to add new user but subscription plan limit was reached.", orgId);
                return RedirectToAction("UsersManagement");
            }

            model.OrganizationId = orgId;
            var userId = await _userLogic.AddOrganizationUser(model);            
        }
        catch (UserAlreadyExistsException)
        {
            await this.PutOrganizationUsersToViewDataAsync();
            model.Errors.Add(_localizer["User with email already exists"]);
            return View(model);
        }
        catch (ValidationException exc)
        {
            await this.PutOrganizationUsersToViewDataAsync();
            ModelState.AddModelError(exc.Property, exc.Message);
            model.Errors.Add(_localizer["Validation error"]);
            return View(model);
        }
        catch (Exception ex)
        {
            await this.PutOrganizationUsersToViewDataAsync();
            _logger.LogError(ex, "Unknown error when trying to register user");
            model.Errors.Add(_localizer["Unknown error"]);
            return View(model);
        }

        return RedirectToAction("UsersManagement");
    }

    [HttpGet("usersmanagementedit/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UsersManagementEditAsync([FromRoute]Guid id)
    {
        // TODO: validate if user can/access this uses/ if they belong to the same organization
     
        var user = await _userLogic.GetAsync(id);
        var model = new UserEditFormModel
        {
            Id = user!.Id,
            Email = user!.Email,
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsersManagementEditSaveAsync(UserEditFormModel model)
    {
        if (!ModelState.IsValid)
        {
            model.LoadErrors(ModelState);
            return View(model);
        }

        try
        {
            var org = await _userLogic.GetOrganizationForUserAsync(model.Id);            
            var organizationId = User.GetOrganizationId();
            if (org.Organization.Id != organizationId)
            {
                return Unauthorized();
            }
            
            await _userLogic.UpdateAsync(model.Id, model);
        }
        catch (UserAlreadyExistsException)
        {
            model.Errors.Add(_localizer["User with email already exists"]);
            return View("UsersManagementEdit", model); // crap in .net framework, need to force by hammer
        }
        catch (ValidationException exc)
        {
            ModelState.AddModelError(exc.Property, exc.Message);
            model.Errors.Add(_localizer["Validation error"]);
            return View("UsersManagementEdit", model); // crap in .net framework, need to force by hammer
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unknown error when trying to edit user.");
            model.Errors.Add(_localizer["Unknown error"]);
            return View("UsersManagementEdit", model); // crap in .net framework, need to force by hammer
        }

        return RedirectToAction("UsersManagement");
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsersManagementEditDelete(UserEditFormModel model)
    {
        try
        {
            var org = await _userLogic.GetOrganizationForUserAsync(model.Id);
            var organizationId = User.GetOrganizationId();
            if (org.Organization.Id != organizationId)
            {
                return Unauthorized();
            }

            if (User.GetUserId() == model.Id)
            {
                return Conflict();
            }

            await _userLogic.DeleteUserAsync(model.Id, organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unknown error when trying to delete user.");
            model.Errors.Add(_localizer["Unknown error"]);
            return View("UsersManagementEdit", model); // crap in .net framework, need to force by hammer
        }

        return RedirectToAction("UsersManagement");
    }

    private async Task<IList<Domain.Models.DTO.OrganizationUserDTO>> PutOrganizationUsersToViewDataAsync()
    {
        var orgId = this.User.GetOrganizationId();
        var users = await _organizationLogic.GetOrganizationUsersAsync(orgId);

        ViewData["OrganizationUsersList"] = users;

        return users;
    }

    public IActionResult Help()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error(int? statusCode)
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, StatusCode = statusCode });
    }

    [AllowAnonymous]
    [HttpPost("notifypayment/{id}")]
    public async Task<IActionResult> NotificationWebhookAsync([FromRoute] string id)
    {
        // TODO: fire and forget but should push message to queue and continue processin on another thread 
        var content = "";
        try
        {
            var ip = Request.GetClientIp();
            content = await new StreamReader(Request.Body).ReadToEndAsync();

            _logger.LogInformation("notifypayment/{id} was called from IP: {ip}", id, ip);

            await _paymentLogic.HandleNotificationAsync(id, content, Request.Headers, ip);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "notifypayment/{id} failed, content was: {content}", id, content);
        }

        return Ok();
    }
}
