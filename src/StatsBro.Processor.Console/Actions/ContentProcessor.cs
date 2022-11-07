namespace StatsBro.Processor.Console.Actions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StatsBro.Domain.Config;
using StatsBro.Domain.Models;
using StatsBro.Processor.Console.Service;
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
        this.CleanUpQueryParams(payload);
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

    private void CleanUpQueryParams(SiteVisitData payload)
    {
        var allowedKeys = this._sitesConfig.GetAllowedQueryParams(payload.Domain);
        var newUrl = RemoveQueryParams(payload.Url, allowedKeys);
        var newReferrer = newUrl;
        if (payload.Url != payload.Referrer)
        {
            newReferrer = RemoveQueryParams(payload.Referrer, allowedKeys);
        }

        payload.Url = newUrl;
        payload.Referrer = newReferrer;
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
