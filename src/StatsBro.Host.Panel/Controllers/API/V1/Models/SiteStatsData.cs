namespace StatsBro.Host.Panel.Controllers.API.V1.Models;

[Serializable]
public class SiteStatsData
{
    public SiteStatsSummary Summary { get; set; } = null!;
    public IEnumerable<KeyValuePairTime> UsersOverTime { get; set; } = null!;
    public IEnumerable<KeyValuePairTime> EngagedUsersOverTime { get; set; } = null!;
    public IEnumerable<KeyValuePairTime> PageViewsOverTime { get; set; } = null!;
    public IEnumerable<KeyValuePairTime> AverageTimeOnSiteOverTime { get; set; } = null!;
    public IList<KeyValuePair<string, long>> TrafficSource { get; set; } = null!;
    public IList<KeyValuePair<string, long>> Browsers { get; set; } = null!;
    public IList<KeyValuePair<string, long>> OSs { get; set; } = null!;
    public IList<KeyValuePair<string, long>> TouchScreens { get; set; } = null!;
    public IList<KeyValuePair<string, long>> ScreenSizes { get; set; } = null!;
    public IList<KeyValuePair<string, long>> Campaigns { get; set; } = null!;
    public IList<KeyValuePair<string, long>> EntryPages { get; set; } = null!;
    public IList<KeyValuePair<string, long>> Languages { get; set; } = null!;
    public IList<KeyValuePair<string, long>> PagesWithMostEngagement { get; set; } = null!;
    public IList<KeyValuePair<string, long>> PageViews { get; set; } = null!;
    public IList<KeyValuePair<string, long>> Countries { get; set; } = null!;
    public IList<KeyValuePair<string, long>> Cities { get; set; } = null!;
    public IList<KeyValuePair<string, long>> Events { get; set; } = null!;
}

[Serializable]
public class SiteStatsSummary
{
    public long TotalUsersCount { get; set; }
    public int? TotalUsersChange { get; set; }
    public long TotalEngagedUsersCount { get; set; }
    public int? TotalEngagedUsersChange { get; set; }
    public long TotalPageViews { get; set; }
    public long TotalCustomEvents { get; set; }
    public int? TotalPageViewChange { get; set; }
    public TimeSpan AverageVisitLength { get; set; }
    public int? AverageVisitLengthChange { get; set; }
}

[Serializable]
public struct KeyValuePairTime
{
    public DateTimeOffset Time { get; set; }

    public long Value { get; set; }
}