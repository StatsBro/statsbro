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
using StatsBro.Domain.Models.DTO;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Domain.Service.Sendgrid;
using StatsBro.Host.Panel.Models.Forms;
using StatsBro.Host.Panel.Models.Forms.Settings;
using StatsBro.Host.Panel.Models.Objects;
using StatsBro.Host.Panel.Services;
using StatsBro.Storage.Database;
using System.Data;
using System.Security.Claims;

public class UserLogic
{
    public const string DEFAULT_PERSIST_QUERY_PARAMS = "ref,source,utm_campaign,utm_medium,utm_source,utm_content,utm_term";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDbRepository _repository;
    private readonly IMessagingService _messagingService;
    private readonly SiteLogic _siteLogic;
    private readonly ReferralLogic _referralLogic;
    private readonly INotificationService _notification;
    private readonly SendgridService _sendgridService; // TODO: create interface on it and abstract it
    private readonly ILogger<UserLogic> _logger;

    public UserLogic(
        IHttpContextAccessor httpContextAccessor,
        IDbRepository repository,
        IMessagingService messagingService,
        INotificationService notification,
        SiteLogic siteLogic,
        ReferralLogic referralLogic,
        SendgridService sendgridService,
        ILogger<UserLogic> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _repository = repository;
        _messagingService = messagingService;
        _notification = notification;
        _siteLogic = siteLogic;
        _referralLogic = referralLogic;
        _sendgridService = sendgridService;
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

        var transaction = await _repository.BeginTransactionAsync();
        try
        {
            var organizationId = await _repository.CreateOrganizationAsync(
                new Organization
                {
                    Name = registerFormModel.Email,
                    SubscriptionType = SubscriptionType.Trial,
                    SubscriptionValidTo = DateTime.UtcNow.AddDays(31).AddMilliseconds(-1),
                }, transaction);

            var userId = await this.AddUserAsync(registerFormModel.Email, registerFormModel.Password, transaction);

            var site = await _siteLogic.SaveSiteAsync(
                userId,
                organizationId,
                new GeneralSettingsFormModel
                {
                    SiteUrl = registerFormModel.SiteUrl,
                    PersistQueryParamsList = DEFAULT_PERSIST_QUERY_PARAMS,
                },
                transaction);

            var role = OrganizationUserRole.Admin;
            await _repository.AssignUserToOrganizationAsync(userId, organizationId, role, transaction);

            await _repository.CommitTransactionAsync(transaction); // COMMIT

            user = await _repository.GetUserAsync(userId);
            await CreateSessionAsync(user!.Id, user.Email, organizationId, role);
            var referralId = await _referralLogic.MakeReferralAsync(_httpContextAccessor.HttpContext?.Request, organizationId);
            _messagingService.NewUserRegistrationAsync(user!, referralId);

            return site.Id;
        }
        catch (Exception)
        {
            await _repository.RollbackTransactionAsync(transaction);
            throw;
        }
    }

    // TODO: parametrize hosting URL
    public async Task SendMagicLinkAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var user = await _repository.GetUserAsync(email);
        if (user == null)
        {
            return;
        }

        var magicLink = new MagicLinkDTO 
        {
            Id = Guid.NewGuid(),
            Origin = MagicLinkOrigin.Login, 
            ValidTo = DateTime.UtcNow.AddDays(1),
            UserId = user.Id,
        };

        var ml = new MagicLinkStruct 
        {
            Email = email, 
            Id = magicLink.Id,
            Timestamp = magicLink.ValidTo.ToFileTimeUtc() 
        };

        await _repository.AddMagicLinkAsync(magicLink);
        var mlEncoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ml.ToString()));
        var magicLinkUrl = $"https://app.statsbro.io/loginlink/{mlEncoded}";

        await _sendgridService.SendLoginByMagicLinkAsync(
            email, 
            new EmailPayloadMagicLink { Link = magicLinkUrl }
            );
    }

    public async Task<Guid> AddOrganizationUser(OrganizationUserFormModel model)
    {
        var user = await _repository.GetUserAsync(model.Email.Trim());
        if (user != null)
        {
            throw new UserAlreadyExistsException();
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            throw new ValidationException(nameof(model.Password), "Password cannot be empty");
        }

        var transaction = await _repository.BeginTransactionAsync();
        try
        {
            var userId = await this.AddUserAsync(model.Email, model.Password, transaction);
            var role = OrganizationUserRole.ReadOnly;
            await _repository.AssignUserToOrganizationAsync(userId, model.OrganizationId, role, transaction);

            await _repository.CommitTransactionAsync(transaction);

            return userId;
        }
        catch 
        {
            await _repository.RollbackTransactionAsync(transaction);
            throw;
        }
    }

    private async Task<Guid> AddUserAsync(string email, string password, IDbTransaction transaction = null!)
    {
        var salt = Chaos.GenerateSalt();
        var passwordHash = Chaos.Hash(salt, password);
        var userId = await _repository.CreateUserAsync(
            new User
            {
                Email = email,
                PasswordSalt = salt,
                PasswordHash = passwordHash,
                RegisteredAt = DateTime.UtcNow
            },
            transaction);

        return userId;
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
            // assumption for now is that user can belong to only one organization
            var orgUser = await _repository.GetOrganizationForUserIdAsync(user.Id);
            await CreateSessionAsync(user.Id, user.Email, orgUser.Organization.Id, orgUser.Role);
            await _repository.UserLoggedInAsync(user.Id);
            return;
        }
        else
        {
            throw new InvalidCredentialsException();
        }
    }

    public async Task LoginByMagicLinkAsync(MagicLinkStruct ml)
    {
        var magicLink = await _repository.GetMagicLinkAsync(ml.Id);
        if (magicLink == null || magicLink.ValidTo < DateTime.UtcNow)
        {
            throw new InvalidCredentialsException();
        }

        var user = await _repository.GetUserAsync(ml.Email);
        if (user == null || magicLink.UserId != user.Id)
        {
            throw new InvalidCredentialsException();
        }

        var orgUser = await _repository.GetOrganizationForUserIdAsync(user.Id);
        await CreateSessionAsync(user.Id, user.Email, orgUser.Organization.Id, orgUser.Role);
        await _repository.UserLoggedInAsync(user.Id);
    }

    public async Task<OrganizationUserDetailsDTO> GetOrganizationForUserAsync(Guid userId)
    {
        return await _repository.GetOrganizationForUserIdAsync(userId);
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

    public async Task DeleteUserAsync(Guid userId, Guid organizationId)
    {
        await _repository.DeleteUserAsync(userId, organizationId);
    }

    private async Task CreateSessionAsync(Guid userId, string email, Guid organizationId, OrganizationUserRole role)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Sid, userId.ToString()),
                new Claim(ClaimTypes.GroupSid, organizationId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString()),
            };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await _httpContextAccessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));
    }
}
