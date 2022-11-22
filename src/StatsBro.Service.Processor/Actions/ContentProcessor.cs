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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

public interface IContentProcessor
{
    SiteVisitData Act(SiteVisitData payload);
}

public class ContentProcessor : IContentProcessor
{
    private readonly ServiceConfig _serviceConfig;
    private readonly ISitesConfigurations _sitesConfig;
    private readonly string _pepper;
    private readonly SHA256 _sha256Hash;
    private readonly ILogger _logger;  

    public ContentProcessor(
        ISitesConfigurations sitesConfig,
        IOptions<ServiceConfig> serviceConfigOptions, 
        ILogger<ContentProcessor> logger)
    {
        this._sitesConfig = sitesConfig;
        this._serviceConfig = serviceConfigOptions.Value;
        this._logger = logger;
        this._sha256Hash = SHA256.Create();

        if (this._serviceConfig.Pepper != "")
        {
            this._pepper = this._serviceConfig.Pepper;
        }

        using var store = new X509Store(StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindBySubjectName, this._serviceConfig.PepperCertSubjectName, false);
        if (certs.Count == 0)
        {
            this._logger.LogError("Certificate is missing, encryption will not be so strong");
            this._pepper = "111111111111222222222223333333333344444444444455555555555555666666666666666666";
        } 
        else
        {
            var certificatePrivateKey = certs[0].GetRSAPrivateKey();
            if (certificatePrivateKey == null)
            {
                this._logger.LogError("Certificate does not have a private RSA key, encryption will not be so strong");
                this._pepper = "111111111111222222222223333333333344444444444455555555555555666666666666666666";
            }
            else
            {
                // New CERT with keys: https://www.scottbrady91.com/openssl/creating-rsa-keys-using-openssl
                using var hmac = new HMACSHA512();
                var shaHash = this._sha256Hash.ComputeHash(certificatePrivateKey.ExportRSAPrivateKey());
                var hmacHash = hmac.ComputeHash(certificatePrivateKey.ExportRSAPrivateKey());
                this._pepper = Convert.ToBase64String(shaHash.Concat(hmacHash).ToArray());                
            }
        }
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
        var rawString = string.Join('|', this._pepper, payload.WindowWidth, payload.WindowHeight, payload.TouchPoints, payload.IP, payload.UserAgent, payload.Domain);
        var bytes = this._sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawString));
        payload.Hash = Convert.ToBase64String(bytes, 0, bytes.Length);
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

    // TODO: remember that allowedKeys in DB should be lowecased
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
