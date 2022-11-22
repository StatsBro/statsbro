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
﻿namespace StatsBro.Service.Processor.Actions;

using StatsBro.Domain.Models;
using StatsBro.Service.Processor.Service;

public interface IEventPyloadFilter
{
    SiteVisitData? Act(SiteVisitData? payload);
}

public class FilterEventPayload : IEventPyloadFilter
{
    private readonly ISitesConfigurations _sitesConfig;

    public FilterEventPayload(ISitesConfigurations sitesConfig)
    {
        this._sitesConfig = sitesConfig;
    }

    public  SiteVisitData? Act(SiteVisitData? payload)
    {
        if (payload == null)
        {
            return null;
        }

        ExtractDomain(payload);
        if(!this._sitesConfig.IsDomainActve(payload.Domain))
        {
            return null;
        }

        if(!string.IsNullOrWhiteSpace(payload.IP) 
            && this._sitesConfig.GetIgnoredIPs(payload.Domain).Contains(payload.IP))
        {
            return null;
        }

        return payload;
    }

    private static void ExtractDomain(SiteVisitData payload)
    {
        var host = new Uri(payload.Url).Host;
        payload.Domain = host;
    }
}
