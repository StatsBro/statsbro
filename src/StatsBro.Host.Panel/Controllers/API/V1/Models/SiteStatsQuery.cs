namespace StatsBro.Host.Panel.Controllers.API.V1.Models;

using FluentValidation;
using StatsBro.Domain.Models.Request;

public class SiteStatsQuery
{
    /// <summary>
    /// [Optional]
    /// The date in the past to include data that has been collected on or after  this point in time (inclusive). Use the format "yyyy-MM-ddThh:mm:ss". Value is treated as UTC.
    /// Default: `-30 days`
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// [Optional]
    /// The date to include data that has been collected on or before this poing in time (inclusive). Use the format "yyyy-MM-ddThh:mm:ss". Value is treated as UTC.
    /// Example: 2022-12-25T16:25:00
    /// Default: `now`
    /// </summary>
    public DateTime? To { get; set; }
    
    /// <summary>
    /// [Optional]
    /// Filter to return only data related to given page. Value is a 'key' from result: `entryPages`
    /// Example: /index.html
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// [Optional]
    /// Filter to return only data related to given city. Value is a 'key' from result: `cities`
    /// Example: Chicago
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// [Optional]
    /// Filter to return only data related to given country. Value is a 'key' from result: `countries`
    /// Example: Brazil
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// [Optional]
    /// Filter to return only data related to given campain. Value is a 'key' from result: `campaigns`
    /// Example: christmas_1
    /// </summary>
    public string? UtmCampaign { get; set; }
    
    /// <summary>
    /// [Optional]
    /// Filter to return only data related to given language. Value is a 'key' from result: `languages`
    /// Example: en
    /// </summary>
    public string? Lang { get; set; }

    /// <summary>
    /// [Optional]
    /// Filter to return only data related to given event. Value is a 'key' from result: `events`
    /// Example: pageview
    /// </summary>
    public string? Event { get; set; }

    /// <summary>
    /// [Optional]
    /// Filter to return only data related to given referrer. Value is a 'key' from result: `traficSource`
    /// Example: linkedin.com
    /// </summary>
    public string? Referrer { get; set; }

    public SiteStatsQuery()
    {
        From = DateTime.Now.Date.AddDays(-30);
        To = DateTime.Now.Date;
    }

    public StatsRequest ToStatsRequest()
    {
        return new StatsRequest
        {
            From = this.From,
            To = this.To,
            Url = this.Url,
            City = this.City,
            Country = this.Country,
            UtmCampaign = this.UtmCampaign,
            Lang = this.Lang,
            Event = this.Event,
            Referrer = this.Referrer,
        };
    }
}


public class SiteStatsQueryValidator : AbstractValidator<SiteStatsQuery>
{
    public SiteStatsQueryValidator()
    {
        RuleFor(x => x.Event).MaximumLength(64);
        RuleFor(x => x.Lang).MaximumLength(6);
        RuleFor(x => x.Country).MaximumLength(24);
        RuleFor(x => x.UtmCampaign).MaximumLength(64);
    }
}