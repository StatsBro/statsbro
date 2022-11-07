namespace StatsBro.Panel.Extensions;

using StatsBro.Domain.Models.Exceptions;
using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));

        var claimSid = principal.FindFirst(ClaimTypes.Sid);
        if (claimSid == null)
        {
            throw new EntityNotFoundException("User data not found");
        }

        return Guid.Parse(claimSid.Value);
    }
}
