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

using Microsoft.AspNetCore.Mvc;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using StatsBro.Domain.Models.DTO;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Host.Panel.Models.Forms.Settings;
using StatsBro.Host.Panel.Services;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Data;
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

    public async Task<IList<Site>> GetSitesAsync(Guid userId, Guid organizationId)
    {
        var sites = await _repository.GetSitesForOrganizationAsync(organizationId, userId);
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

    public async Task<Site?> GetSiteAsync(Guid userId, Guid organizationId, Guid siteId)
    {
        var sites = await _repository.GetSitesForOrganizationAsync(organizationId, userId); // TODO: optimalization: get single
        var site = sites.FirstOrDefault(s => s.Id == siteId);
        if (site != null)
        {
            var domainDocuments = await _repositoryES.HasDomainDocumentsAsync(new List<string> { site.Domain }, TimeSpan.FromDays(7));
            if (domainDocuments.TryGetValue(site.Domain, out var hasDocuments))
            {
                site.IsScriptLive = hasDocuments;
            }
        }

        return site;
    }

    public async Task<Site?> GetSiteAsync(Guid siteId)
    {
        var site = await _repository.GetSiteAsync(siteId);
        if (site != null)
        {
            var domainDocuments = await _repositoryES.HasDomainDocumentsAsync(new List<string> { site.Domain }, TimeSpan.FromDays(7));
            if (domainDocuments.TryGetValue(site.Domain, out var hasDocuments))
            {
                site.IsScriptLive = hasDocuments;
            }
        }

        return site;
    }

    public async Task<Site?> GetSiteAsync(string shareId)
    {
        var result = await _repository.GetSiteByShareIdAsync(shareId);
        return result;
    }

    public async Task<Site> AddNewSiteAsync(Guid userId, Guid organizationId, string siteUrl)
    {
        Uri uri;
        try
        {
            uri = new Uri(siteUrl);
        }
        catch (UriFormatException)
        {
            throw new ValidationException(nameof(siteUrl), "Site url is incorrect");
        }

        if (await CheckIfSiteUrlRegistered(siteUrl))
        {
            throw new ValidationException(nameof(siteUrl), "Site url is incorrect");
        }

        var site = await SaveSiteAsync(
            userId,
            organizationId,
            new GeneralSettingsFormModel
            {
                SiteUrl = siteUrl,
                PersistQueryParamsList = UserLogic.DEFAULT_PERSIST_QUERY_PARAMS,
            });

        return site;
    }

    public async Task<Site> SaveSiteAsync(
        Guid userId,
        Guid organizationId,
        GeneralSettingsFormModel model,
        IDbTransaction transaction = null!)
    {
        var sites = await _repository.GetSitesForOrganizationAsync(organizationId, userId, transaction);
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
            OrganizationId = organizationId,
            Domain = uri.Host,
            IgnoreIPsList = ignoreIPs.Distinct().ToList(),
            PersistQueryParamsList = persistQueryParamsList.Distinct().ToList(),
        }, transaction);

        await this._notification.NotifySiteConfigChangedAsync(siteId);

        return await _repository.GetSiteAsync(siteId, transaction);
    }

    public async Task<bool> CheckIfSiteUrlRegistered(string siteUrl)
    {
        var site = await _repository.GetSiteAsync(new Uri(siteUrl).Host);
        return site != null;
    }

    public async Task<SharingSettingsFormModel> GetSiteSharingSettingsAsync(Guid siteId, IUrlHelper urlHelper)
    {
        try
        {
            var dtoSharing = await _repository.GetSiteSharingSettingsAsync(siteId);
            var dtoApiKey = await _repository.GetSiteApiSettingsAsync(siteId);

            return new SharingSettingsFormModel
            {
                Id = siteId,
                LinkSharingEnabled = !string.IsNullOrEmpty(dtoSharing?.LinkShareId),
                LinkSharingUrl = !string.IsNullOrEmpty(dtoSharing?.LinkShareId) ? urlHelper.Action(action: "Share", controller: "Stats", values: new { shareId = dtoSharing.LinkShareId }, protocol: "https") : null,
                ApiKeyEnabled = !string.IsNullOrEmpty(dtoApiKey?.ApiKey),
                ApiKeyValue = dtoApiKey?.ApiKey,
            };
        }
        catch(Exception)
        {
            throw;
        }
    }

    public async Task SaveSiteSharingSettingsAsync(SharingSettingsFormModel model)
    {
        try
        {
            var dto = await _repository.GetSiteSharingSettingsAsync(model.Id);

            if (dto == null)
            {
                dto = new SiteSharingSettingsDTO
                {
                    SiteId = model.Id.ToString().ToUpper(),
                    LinkShareId = model.LinkSharingEnabled ? Chaos.GenerateSalt(9) : null,
                };
            }

            if (model.LinkSharingEnabled && string.IsNullOrEmpty(dto.LinkShareId))
            {
                dto.LinkShareId = Chaos.GenerateSalt(9);
            }
            if (!model.LinkSharingEnabled)
            {
                dto.LinkShareId = null;
            }

            await _repository.SaveSiteSharingSettingsAsync(dto);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<SiteApiSettingsDTO?> GetSiteApiSettingsAsync(string apiKey)
    {
        var dto = await _repository.GetSiteApiSettingsAsync(apiKey);
        return dto;
    }

    public async Task SaveSiteApiSettingsAsync(SharingSettingsFormModel model)
    {
        const int keySize = 64;

        try
        {
            var dto = await _repository.GetSiteApiSettingsAsync(model.Id);

            if (dto == null)
            {
                dto = new SiteApiSettingsDTO
                {
                    SiteId = model.Id.ToString().ToUpper(),
                    ApiKey = model.ApiKeyEnabled ? Chaos.GetUniqueAlphanumericString(keySize) : null,
                };
            }

            if (model.ApiKeyEnabled && string.IsNullOrEmpty(dto.ApiKey))
            {
                dto.ApiKey = Chaos.GetUniqueAlphanumericString(keySize);
            }
            if (!model.ApiKeyEnabled)
            {
                dto.ApiKey = null;
            }

            await _repository.SaveSiteApiSettingsAsync(dto);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<ProcessedEventRaw>> GetRawDataAsync(Guid userId, Guid organizationId, Guid siteId, DateTimeOffset from, DateTimeOffset to)
    {
        var sites = await _repository.GetSitesForOrganizationAsync(organizationId, userId);
        if (!sites.Any(s => s.Id == siteId))
        {
            throw new UnauthorizedAccessException();
        }

        var site = sites.Where(s => s.Id == siteId).First();

        var data = await _repositoryES.GetRawDataAsync(site.Domain, from, to);
        return data;
    }

    public async Task<string> GetRawDataFileAsync(Guid userId, Guid organizationId, Guid siteId, DateTimeOffset from, DateTimeOffset to)
    {
        var data = await GetRawDataAsync(userId, organizationId, siteId, from, to);
        var result = new StringBuilder();

        result.Append("url_scheme;url_domain;url_path;referrer_scheme;referrer_domain;referrer_path;window_width;window_height;touch_points;lang;hash;domain;event;timestamp;is_touch_screen;screen_size;script_version;user_agent_name;user_agent_version;os_name;os_version;device_name;device_version;visit_time\r\n");

        foreach (var ev in data)
        {
            result.Append(ev.Url.Scheme);
            result.Append(';');
            result.Append(ev.Url.Domain);
            result.Append(';');
            result.Append(ev.Url.Path);
            result.Append(';');
            result.Append(ev.Referrer?.Scheme);
            result.Append(';');
            result.Append(ev.Referrer?.Domain);
            result.Append(';');
            result.Append(ev.Referrer?.Path);
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
            result.Append(ev.UserAgent?.Name);
            result.Append(';');
            result.Append(ev.UserAgent?.Version);
            result.Append(';');
            result.Append(ev.UserAgent?.Os?.Name);
            result.Append(';');
            result.Append(ev.UserAgent?.Os?.Version);
            result.Append(';');
            result.Append(ev.UserAgent?.Device?.Name);
            result.Append(';');
            result.Append(ev.UserAgent?.Device?.Version);
            result.Append(';');
            //result.Append(ev.TimeSpentMillis);
            //result.Append("\r\n");
        }

        return result.ToString();
    }
}
