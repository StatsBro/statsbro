namespace StatsBro.Processor.Console.Actions;

using StatsBro.Domain.Models;
using StatsBro.Processor.Console.Service;

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
