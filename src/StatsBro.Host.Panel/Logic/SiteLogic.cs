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

using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Host.Panel.Models.Forms;
using StatsBro.Host.Panel.Services;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Net;
using System.Text;

public class SiteLogic 
{
    private readonly IDbRepository _repository;
    private readonly IEsRepository _repositoryES;
    private readonly INotificationService _notification;

    public SiteLogic(IDbRepository repository, IEsRepository esRepository, INotificationService notification)
    {
        _repository = repository;
        _repositoryES = esRepository;
        _notification = notification;
    }

    public async Task<IList<Site>> GetSitesAsync(Guid userId)
    {
        var sites = await _repository.GetSitesForUserAsync(userId);
        var domains = sites.Select(s => s.Domain).ToList();
        IDictionary<string, bool> domainDocuments = await _repositoryES.HasDomainDocumentsAsync(domains, TimeSpan.FromDays(7));
        foreach(var s in sites)
        {
            if(domainDocuments.TryGetValue(s.Domain, out var hasDocuments))
            {
                s.IsScriptLive = hasDocuments;
            }
        }

        return sites;
    }

    public async Task<Site?> GetSiteAsync(Guid userId, Guid siteId)
    {
        var sites = await _repository.GetSitesForUserAsync(userId); // TODO: optimalization: get single

        return sites.FirstOrDefault(s => s.Id == siteId);
    }

    public async Task<Site> SaveSiteAsync(Guid userId, SiteSettingsFormModel model)
    {
        var sites = await _repository.GetSitesForUserAsync(userId);
        if (model.Id != Guid.Empty && !sites.Any(s => s.Id == model.Id))
        {
            throw new UnauthorizedAccessException();
        }

        var ignoreIPsList = string.IsNullOrWhiteSpace(model.IgnoreIPsList) 
            ? new List<string>() 
            : new List<string>(model.IgnoreIPsList.Split(Consts.SiteSettingsIPsSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        
        var ignoreIPs = new List<IPAddress>();
        foreach (var ipStr in ignoreIPsList)
        {
            if(IPAddress.TryParse(ipStr, out var parsedIp))
            {
               ignoreIPs.Add(parsedIp); 
            }
            else
            {
                throw new ValidationException(nameof(model.IgnoreIPsList), $"value: {ipStr} is not a valid IP address");
            }
        }

        var persistQueryParamsList = string.IsNullOrWhiteSpace(model.PersistQueryParamsList)
            ? new List<string>()
            : new List<string>(model.PersistQueryParamsList.Split(Consts.SiteSettingsQueryParamsSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLowerInvariant()));

        var uri = new Uri(model.SiteUrl);
        var siteId = await _repository.SaveSiteAsync(new Site
        {
            Id = model.Id,
            UserId = userId,
            Domain = uri.Host,
            IgnoreIPsList = ignoreIPs.Distinct().ToList(),
            PersistQueryParamsList = persistQueryParamsList.Distinct().ToList(),
        });

        await this._notification.NotifySiteConfigChangedAsync(siteId);

        return await _repository.GetSiteAsync(siteId);
    }

    public async Task<bool> CheckIfSiteUrlRegistered(string siteUrl)
    {
        var site = await _repository.GetSiteAsync(new Uri(siteUrl).Host);
        return site != null;
    }

    public async Task<string> GetRawDataAsync(Guid userId, Guid siteId, DateTimeOffset from, DateTimeOffset to)
    {
        var sites = await _repository.GetSitesForUserAsync(userId);
        if (!sites.Any(s => s.Id == siteId))
        {
            throw new UnauthorizedAccessException();
        }

        var site = sites.Where(s => s.Id == siteId).First();

        var data = await _repositoryES.GetRawDataAsync(site.Domain, from, to);
        var result = new StringBuilder();

        result.Append("url_scheme;url_domain;url_path;referrer_scheme;referrer_domain;referrer_path;window_width;window_height;touch_points;lang;hash;domain;event;timestamp;is_touch_screen;screen_size;script_version;user_agent_name;user_agent_version;os_name;os_version;device_name;device_version\r\n");

        foreach (var ev in data)
        {
            result.Append(ev.Url.Scheme);
            result.Append(';');
            result.Append(ev.Url.Domain);
            result.Append(';');
            result.Append(ev.Url.Path);
            result.Append(';');
            result.Append(ev.Referrer.Scheme);
            result.Append(';');
            result.Append(ev.Referrer.Domain);
            result.Append(';');
            result.Append(ev.Referrer.Path);
            result.Append(';');
            result.Append(ev.WindowWidth);
            result.Append(';');
            result.Append(ev.WindowHeight);
            result.Append(';');
            result.Append(ev.TouchPoints);
            result.Append(';');
            result.Append(ev.Lang);
            result.Append(';');
            result.Append(ev.Hash);
            result.Append(';');
            result.Append(ev.Domain);
            result.Append(';');
            result.Append(ev.EventName);
            result.Append(';');
            result.Append(ev.Timestamp);
            result.Append(';');
            result.Append(ev.IsTouchScreen);
            result.Append(';');
            result.Append(ev.ScreenSize);
            result.Append(';');
            result.Append(ev.ScriptVersion);
            result.Append(';');
            result.Append(ev.UserAgent.Name);
            result.Append(';');
            result.Append(ev.UserAgent.Version);
            result.Append(';');
            result.Append(ev.UserAgent.Os?.Name);
            result.Append(';');
            result.Append(ev.UserAgent.Os?.Version);
            result.Append(';');
            result.Append(ev.UserAgent.Device?.Name);
            result.Append(';');
            result.Append(ev.UserAgent.Device?.Version);
            result.Append(";\r\n");
        }

        return result.ToString();
    }
}
