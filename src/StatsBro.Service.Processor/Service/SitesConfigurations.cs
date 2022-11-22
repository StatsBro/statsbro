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
﻿namespace StatsBro.Service.Processor.Service;

using Microsoft.Extensions.Logging;
using StatsBro.Domain.Helpers;
using StatsBro.Storage.Database;
using System.Reactive.Linq;

public interface ISitesConfigurations
{
    bool IsDomainActve(string domain);

    string[] GetAllowedQueryParams(string domain);

    string GetIgnoredIPs(string domain);

    Task Initialize();
}

public  class SitesConfigurations : ISitesConfigurations
{
    private readonly IDbRepository _dbRepository;
    private readonly IConfigReloadNotifier _configReloadNotifier;
    private readonly ILogger _logger;

    private readonly SemaphoreSlim _locker = new(1);
    
    private Dictionary<string, string> _siteIgnoredIps = new();
    private Dictionary<string, string[]> _queryParams = new();
    private Dictionary<string, bool> _sites= new();

    public SitesConfigurations(IDbRepository dbRepository, IConfigReloadNotifier configReloadNotifier, ILogger<SitesConfigurations> logger)
    {
        this._dbRepository = dbRepository;
        this._configReloadNotifier = configReloadNotifier;
        this._logger = logger;
    }

    public async Task Initialize()
    {
        this._configReloadNotifier.SubscribeToReloadConfig(this.ReloadConfig);
        await this.ReloadConfig();
    }

    public bool IsDomainActve(string domain)
    {
        return this._sites.ContainsKey(domain);
    }
    
    public string[] GetAllowedQueryParams(string domain)
    {
        if(this._queryParams.TryGetValue(domain, out var queryParams))
        {
            return queryParams;
        }

        return Array.Empty<string>();
    }

    public string GetIgnoredIPs(string domain)
    {
        if(this._siteIgnoredIps.TryGetValue(domain, out var ignoredIPs))
        {
            return ignoredIPs;
        }

        return string.Empty;
    }

    private async Task ReloadConfig()
    {
        try
        {
            var sitesData = await _dbRepository.GetSitesAsync();
            var ignoreIps = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string[]>();
            var sites = new Dictionary<string, bool>();

            foreach (var site in sitesData)
            {
                ignoreIps.Add(site.Domain, string.Join(Consts.SiteSettingsIPsSeparator, site.IgnoreIPsList));
                queryParams.Add(site.Domain, site.PersistQueryParamsList.ToArray());
                sites.Add(site.Domain, true);
            }

            this._siteIgnoredIps = ignoreIps;
            this._queryParams = queryParams;
            this._sites = sites;

            _logger.LogDebug("Sites config reloaded.");
        }
        finally
        {
            this._locker.Release();
        }
    }

}
