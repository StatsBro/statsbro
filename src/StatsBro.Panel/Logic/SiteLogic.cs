namespace StatsBro.Panel.Logic;

using StatsBro.Domain.Models;
using StatsBro.Domain.Models.Exceptions;
using StatsBro.Panel.Models.Forms;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Net;

public class SiteLogic 
{
    protected readonly IDbRepository _repository;
    protected readonly IEsRepository _repositoryES;

    public SiteLogic(IDbRepository repository, IEsRepository esRepository)
    {
        _repository = repository;
        _repositoryES = esRepository;
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
        var ignoreIPsList = string.IsNullOrWhiteSpace(model.IgnoreIPsList) 
            ? new List<string>() 
            : new List<string>(model.IgnoreIPsList.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        
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
            : new List<string>(model.PersistQueryParamsList.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        var uri = new Uri(model.SiteUrl);
        var siteId = await _repository.SaveSiteAsync(new Site
        {
            Id = model.Id,
            UserId = userId,
            Domain = uri.Host,
            IgnoreIPsList = ignoreIPs,
            PersistQueryParamsList = persistQueryParamsList
        });

        return await _repository.GetSiteAsync(siteId);
    }
}
