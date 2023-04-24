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
﻿namespace StatsBro.Host.Panel.Extensions;

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
            throw new EntityNotFoundException("User data not found.");
        }

        return Guid.Parse(claimSid.Value);
    }

    public static Guid GetOrganizationId(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        var groupSid = principal.FindFirst(ClaimTypes.GroupSid);
        if (groupSid == null)
        {
            throw new EntityNotFoundException("Organization data not found.");
        }

        return Guid.Parse(groupSid.Value);
    }

    public static Guid GetSiteId(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        var siteclaimNameIdentifier = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (siteclaimNameIdentifier == null)
        {
            throw new EntityNotFoundException("Site data not found.");
        }

        return Guid.Parse(siteclaimNameIdentifier.Value);
    }
}
