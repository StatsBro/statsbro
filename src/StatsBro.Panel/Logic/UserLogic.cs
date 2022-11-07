namespace StatsBro.Panel.Logic;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Panel.Models.Forms;
using StatsBro.Panel.Services;
using StatsBro.Storage.Database;
using System.Security.Claims;

public class UserLogic
{
    public const string DEFAULT_PERSIST_QUERY_PARAMS = "ref;source;utm_campaign;utm_medium;utm_source;utm_content;utm_term";

    protected readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly IDbRepository _repository;
    protected readonly IMessagingService _messagingService;
    private readonly SiteLogic _siteLogic;

    public UserLogic(
        IHttpContextAccessor httpContextAccessor,
        IDbRepository repository,
        IMessagingService messagingService,
        SiteLogic siteLogic)
    {
        _httpContextAccessor = httpContextAccessor;
        _repository = repository;
        _messagingService = messagingService;
        _siteLogic = siteLogic;
    }

    public async Task<Guid> RegisterAsync(RegisterFormModel registerFormModel)
    {
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
        await _messagingService.NewUserRegistration(user!);

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
