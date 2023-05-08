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
using StatsBro.Storage.Database;

namespace StatsBro.Host.Panel.Logic;

public class ReferralLogic
{
    public const string ReferralCookieName = "referral";
    public const string ReferralCustomIdCookieName = "referralcid";

    private readonly IDbRepository _repository;
    private readonly ILogger<ReferralLogic> _logger;

    public ReferralLogic(IDbRepository repository, ILogger<ReferralLogic> logger)
    {
        this._repository = repository;
        this._logger = logger;
    }

    public async Task<Guid?> MakeReferralAsync(HttpRequest? request, Guid organizationId)
    {
        if (request == null)
        {
            return null;
        }

        var @ref = GetCookie(request, ReferralCookieName);
        if (string.IsNullOrWhiteSpace(@ref))
        {
            return null;
        }

        try
        {
            var cid = GetCookie(request, ReferralCustomIdCookieName);
            var referralId = await _repository.SaveReferralAsync(organizationId, @ref, cid);
            if(referralId == null)
            {
                _logger.LogWarning("Referral for {oganizationId} not creted. {referralKey}", organizationId, @ref);
            }

            return referralId;
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc, "{methodName} exception, this was for {organizationId}, referralKey: {referralKey}", nameof(MakeReferralAsync), organizationId, @ref);
        }

        return null;
    }

    public static void SetReferralCookies(HttpRequest request, HttpResponse response, int? @ref, string? cid)
    {
        // set cookies
        if (@ref != null && GetCookie(request, ReferralCookieName) == null)
        {
            SetCookie(response, ReferralCookieName, @ref.Value.ToString());
            if (!string.IsNullOrWhiteSpace(cid))
            {
                SetCookie(response, ReferralCustomIdCookieName, cid);
            }
        }
    }

    public static string? GetCookie(HttpRequest request, string name)
    {
        if (request.Cookies.TryGetValue(name, out var cookie) && !string.IsNullOrWhiteSpace(cookie))
        {
            return cookie;
        }

        return null;
    }

    public static void SetCookie(HttpResponse response, string name, string value)
    {
        var options = new CookieOptions
        {
            Expires = DateTime.UtcNow.AddDays(179),
            HttpOnly = false,
            SameSite = SameSiteMode.Strict
        };

        response.Cookies.Append(name, value, options);
    }
}
