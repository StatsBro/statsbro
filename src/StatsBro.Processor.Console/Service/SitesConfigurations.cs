namespace StatsBro.Processor.Console.Service;

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

    private readonly SemaphoreSlim _locker = new(1);
    
    private Dictionary<string, string> _siteIgnoredIps = new();
    private Dictionary<string, string[]> _queryParams = new();
    private Dictionary<string, bool> _sites= new();

    public SitesConfigurations(IDbRepository dbRepository, IConfigReloadNotifier configReloadNotifier)
    {
        this._dbRepository = dbRepository;
        this._configReloadNotifier = configReloadNotifier;
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
        await _locker.WaitAsync(2000);
        try
        {
            var sitesData = await _dbRepository.GetSitesAsync();
            var ignoreIps = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string[]>();
            var sites = new Dictionary<string, bool>();

            foreach (var site in sitesData)
            {
                ignoreIps.Add(site.Domain, string.Join(';', site.IgnoreIPsList));
                queryParams.Add(site.Domain, site.PersistQueryParamsList.ToArray());
                sites.Add(site.Domain, true);
            }

            this._siteIgnoredIps = ignoreIps;
            this._queryParams = queryParams;
            this._sites = sites;
        }
        finally
        {
            this._locker.Release();
        }
    }

}
