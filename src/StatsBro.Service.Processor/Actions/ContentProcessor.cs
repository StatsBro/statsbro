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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StatsBro.Domain.Config;
using StatsBro.Domain.Models;
using StatsBro.Service.Processor.Service;
using System;
using System.Linq;
using System.Web;

public interface IContentProcessor
{
    SiteVisitData Act(SiteVisitData payload);
}

public class ContentProcessor : IContentProcessor
{
    private readonly ISitesConfigurations _sitesConfig;
    private readonly IHashCalculator _hashCalculator;
    

    public ContentProcessor(
        ISitesConfigurations sitesConfig,
        IHashCalculatorFactory hashCalculatorFactory,
        ILogger<ContentProcessor> logger)
    {
        this._sitesConfig = sitesConfig;        
        this._hashCalculator = hashCalculatorFactory.Create();
    }

    public SiteVisitData Act(SiteVisitData payload)
    {
        this.CleanUpQueryParamsForUrl(payload);
        this.CleanUpQueryParamsForReferrer(payload);
        this.CalculateHash(payload);
        CalculateScreenSize(payload);
        CalculateIsTouchScreen(payload);

        return payload;
    }

    private void CalculateHash(SiteVisitData payload)
    {
        payload.Hash = this._hashCalculator.Calculate(payload.WindowWidth, payload.WindowHeight, payload.TouchPoints, payload.IP, payload.UserAgent, payload.Domain);
    }

    private void CleanUpQueryParamsForUrl(SiteVisitData payload)
    {
        var allowedKeys = this._sitesConfig.GetAllowedQueryParams(payload.Domain);
        payload.Url = RemoveQueryParams(payload.Url, allowedKeys);
    }

    private void CleanUpQueryParamsForReferrer(SiteVisitData payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.Referrer)
            && Uri.TryCreate(payload.Referrer, UriKind.Absolute, out var parsedUri))
        {
            var allowedKeys = this._sitesConfig.GetAllowedQueryParams(payload.Domain);
            payload.Referrer = RemoveQueryParams(payload.Referrer, allowedKeys);
        }
    }

    private static string RemoveQueryParams(string url, string[] allowedKeys)
    {
        if (url == null)
        {
            return "";
        }
            
        if(!url.Contains('?'))
        {
            return url;
        }

        var modifiedQueryParams = "";
        string[] urlParts = url.ToLower().Split('?');
        try
        {
            var querystrings = HttpUtility.ParseQueryString(urlParts[1]);
            var keysToRemove = querystrings.AllKeys.Except(allowedKeys);

            foreach (var key in keysToRemove)
            {
                querystrings.Remove(key);
            }

            if (querystrings.Count > 0)
            {
                modifiedQueryParams =
                  "?"
                  + string.Join("&", querystrings.AllKeys.Select(c => c + "=" + querystrings[c]));
            }
        }
        catch (NullReferenceException) { }

        return urlParts[0] + modifiedQueryParams;
    }


    private static void CalculateIsTouchScreen(SiteVisitData payload)
    {
        payload.IsTouchScreen = payload.TouchPoints > 0;
    }

    private static void CalculateScreenSize(SiteVisitData payload)
    {
        payload.ScreenSize = payload.WindowWidth switch
        {
            var w when w < 576 => "extra small",
            var w when w >= 576 && w < 768 => "small",
            var w when w >= 768 && w < 992=> "medium",
            var w when w >= 922 && w < 1200 => "large",
            var w when w >= 1200 && w < 1400 => "extra large",
            var w when w >= 1400 => "extra extra large",
            _ => "unknown"
        };
    }
}
