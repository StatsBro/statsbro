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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Host.Panel.Models.Forms;
using StatsBro.Host.Panel.Services;
using StatsBro.Storage.Database;
using System.Security.Claims;

public class UserLogic
{
    public const string DEFAULT_PERSIST_QUERY_PARAMS = "ref,source,utm_campaign,utm_medium,utm_source,utm_content,utm_term";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDbRepository _repository;
    private readonly IMessagingService _messagingService;
    private readonly SiteLogic _siteLogic;
    private readonly INotificationService _notification;
    private readonly ILogger<UserLogic> _logger;

    public UserLogic(
        IHttpContextAccessor httpContextAccessor,
        IDbRepository repository,
        IMessagingService messagingService,
        INotificationService notification,
        SiteLogic siteLogic,
        ILogger<UserLogic> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _repository = repository;
        _messagingService = messagingService;
        _notification = notification;
        _siteLogic = siteLogic;
        _logger = logger;
    }

    public async Task<Guid> RegisterAsync(RegisterFormModel registerFormModel)
    {
        if (!string.IsNullOrEmpty(registerFormModel.TrapHidden) || !string.IsNullOrEmpty(registerFormModel.TrapNotVisible))
        {
            throw new ValidationException(nameof(registerFormModel.TrapHidden), "Bot prevention.");
        }
        
        if (DateTime.FromFileTimeUtc(registerFormModel.CreatedAt).AddSeconds(3) > DateTime.UtcNow)
        {
            throw new ValidationException(nameof(registerFormModel.CreatedAt), "Bot prevention.");
        }

        if (string.IsNullOrEmpty(registerFormModel.Email) || registerFormModel.Email.Length > 512)
        {
            throw new ValidationException(nameof(registerFormModel.Email), "Email is empty or too long");
        }

        var user = await _repository.GetUserAsync(registerFormModel.Email.Trim());
        if (user != null)
        {
            throw new UserAlreadyExistsException();
        }

        if (string.IsNullOrWhiteSpace(registerFormModel.Password))
        {
            throw new ValidationException(nameof(registerFormModel.Password), "Password cannot be empty");
        }

        Uri uri;
        try
        {
            uri = new Uri(registerFormModel.SiteUrl);
        }
        catch(UriFormatException)
        {
            throw new ValidationException(nameof(registerFormModel.SiteUrl), "Site url is incorrect");
        }

        if (await _siteLogic.CheckIfSiteUrlRegistered(registerFormModel.SiteUrl))
        {
            _logger.LogWarning("User {email} tried to register with alread existing domain: {domain}", registerFormModel.Email, registerFormModel.SiteUrl);
            throw new ValidationException(nameof(registerFormModel.SiteUrl), "Site url is incorrect");
        }

        var salt = Chaos.GenerateSalt();
        var passwordHash = Chaos.Hash(salt, registerFormModel.Password);
        var userId = await _repository.CreateUserAsync(new User { 
            Email = registerFormModel.Email,
            PasswordSalt = salt,
            PasswordHash = passwordHash,
            RegisteredAt = DateTime.UtcNow
        });

        var site = await _siteLogic.SaveSiteAsync(
            userId, 
            new SiteSettingsFormModel { 
                SiteUrl = registerFormModel.SiteUrl,
                PersistQueryParamsList = DEFAULT_PERSIST_QUERY_PARAMS,
            });

        await _repository.AssignUserToSiteAsync(userId, site.Id);

        user = await _repository.GetUserAsync(userId);
        await CreateSessionAsync(user!.Id, user.Email);
        _messagingService.NewUserRegistrationAsync(user!);
        await _notification.NotifySiteConfigChangedAsync(site.Id);

        return site.Id;
    }

    public async Task LoginAsync(LoginFormModel loginFormModel)
    {
        if (string.IsNullOrEmpty(loginFormModel.Email) || loginFormModel.Email.Length > 512)
        {
            throw new InvalidCredentialsException();
        }

        var user = await _repository.GetUserAsync(loginFormModel.Email);
        if (user == null)
        {
            throw new InvalidCredentialsException();
        }

        var h = Chaos.Hash(user.PasswordSalt, loginFormModel.Password);
        if (h == user.PasswordHash)
        {
            await CreateSessionAsync(user.Id, user.Email);

            return;
        }
        else
        {
            throw new InvalidCredentialsException();
        }
    }

    public async Task<User?> GetAsync(Guid id)
    {
        return await _repository.GetUserAsync(id);
    }

    public async Task<User> UpdateAsync(Guid userId, UserEditFormModel model)
    {
        var oldUser = await _repository.GetUserAsync(userId);
        if (oldUser == null)
        {
            throw new EntityNotFoundException("User update not possible, user old data not found");
        }

        if(!string.IsNullOrEmpty(model.Password))
        {
            var newSalt = Chaos.GenerateSalt();
            var newPasswordHash = Chaos.Hash(newSalt, model.Password!);
            await _repository.UpdateUserPasswordAsync(userId, newPasswordHash, newSalt);
        }

        if(oldUser.Email != model.Email)
        {
            try
            {
                var userWithEmail = await _repository.GetUserAsync(model.Email.Trim());
                if (userWithEmail != null)
                {
                    throw new UserAlreadyExistsException();
                }
            }
            catch (EntityNotFoundException) { }

            await this._repository.UpdateUserAsync(new User { Id = userId, Email = model.Email });
        }

        var updatedUserData = await this._repository.GetUserAsync(userId);
        return updatedUserData!;
    }

    private async Task CreateSessionAsync(Guid userId, string email)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Sid, userId.ToString())
            };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await _httpContextAccessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));
    }
}
