namespace StatsBro.Host.Panel.Infrastructure.AuthHandlers.HeaderToken;

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using StatsBro.Host.Panel.Logic;

public class HeaderTokenAuthHandler : AuthenticationHandler<HeaderTokenAuthSchemeOptions>
{
    public const string AuthSchemaName = "ApiKeySchemaName";
    public const string HeaderNameApiKey = "api-key";

    private readonly SiteLogic _siteLogic;

    public HeaderTokenAuthHandler(
            IOptionsMonitor<HeaderTokenAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            SiteLogic siteLogic
        )
            : base(options, logger, encoder, clock)
    {
        _siteLogic = siteLogic;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if(!Request.Headers.ContainsKey(HeaderNameApiKey))
        {
            return AuthenticateResult.Fail("API Key Not Found.");
        }
        
        var apiKey = Request.Headers[HeaderNameApiKey].ToString();        
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            // api-key: c..................................6
            apiKey = apiKey[..Math.Min(128, apiKey.Length)]; // limit the size, everything else is .....

            var siteApiSettings = await _siteLogic.GetSiteApiSettingsAsync(apiKey);
            if (siteApiSettings == null)
            {
                return AuthenticateResult.Fail("Unauthorized.");
            }

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, siteApiSettings.SiteId) };

            var claimsIdentity = new ClaimsIdentity(claims, nameof(HeaderTokenAuthHandler));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), this.Scheme.Name);
            
            return AuthenticateResult.Success(ticket);
        }

        return AuthenticateResult.Fail("Unauthorized.");
    }
}
