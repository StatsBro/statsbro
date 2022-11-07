namespace StatsBro.Panel.Logic;

using StatsBro.Domain.Models;
using StatsBro.Panel.Models;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Data;

public class StatsLogic
{
    private readonly IDbRepository _dbRepository;
    private readonly IEsStatsRepository _repositoryStats;
    private readonly ILogger<StatsLogic> _logger;

    public StatsLogic(IDbRepository dbRepository, IEsStatsRepository esStatsRepository, ILogger<StatsLogic> logger)
    {
        this._dbRepository = dbRepository;
        this._repositoryStats = esStatsRepository;
        this._logger = logger;
    }

    public async Task<StatsViewModel> GetStatsAsync(Site site, string query, CancellationToken cancellationToken)
    {
        var totalUsersCount = await _repositoryStats.TotalUsersCountAsync(site.Domain, query, cancellationToken);
        var totalPageViews = await _repositoryStats.TotalPageviewsCountAsync(site.Domain, query, cancellationToken);
        var generalStats = await _repositoryStats.GeneralStatsAsync(site.Domain, query, cancellationToken);

        // TODO: time range: 
        var stats = new StatsViewModel()
        {
            Query = query,
            Summary = new StatsSummaryViewModel
            {
                TotalUsersCount = totalUsersCount,
                TotalUsersChange = 12,
                TotalEngagedUsersCount = 893,
                TotalEngagedUsersChange = 5,
                TotalPageViews = totalPageViews,
                TotalPageViewChange = -4,
                AverageVisitLength = new TimeSpan(0, 5, 32),
                AverageVisitLengthChange = -7 // minus 7 percent
            },
            UsersInTime = GetRandomTimeSeries("Użytkownicy", "Zaangażowani użytkownicy"),
            PageViewsInTime = GetRandomTimeSeries("Odsłony"),
            AverageVisitLengthInTime = GetRandomTimeSeries("Czas wizyty"),
            TrafficSources = this.GetPieChart(generalStats.Referrers), //GetRandomPieChart("google.com", "github.com", "indiehackers.com"),
            Browsers = this.GetPieChart(generalStats.Browsers), //GetRandomPieChart("Microsoft Edge", "Firefox", "Chrome", "Other"),
            OSs = this.GetPieChart(generalStats.Oses),
            TouchScreens = this.GetPieChart(generalStats.IsTouchScreen),
            ScreenSizes = this.GetPieChart(generalStats.ScreenSize),
            Campaigns = GetRandomTable("query.utm_campaign", query),
            EntryPages = GetRandomTable("referrer", query),
            Languages = GetRandomTable("lang", query),
            PagesWithMostEngagement = GetRandomTable("url.page", query),
            PageViews = GetRandomTable("url.page", query),
            Countries = GetRandomTable("loc.country", query),
            Cities = GetRandomTable("loc.city", query),
            Site = site
        };

        return stats;
    }

    private TableViewModel GetRandomTable(string fieldName, string query)
    {
        var data = new DataTable();
        data.Columns.Add("Nazwa");
        data.Columns.Add("Wyświetlenia");

        var start = Random.Shared.Next(1000, 10000);
        for (int i = 0; i < 10; i++)
        {
            data.Rows.Add($"nazwa_{i}", start--);
        }

        return new TableViewModel { Data = data, FieldName = fieldName, Query = query };
    }

    private PieChartViewModel GetPieChart(IList<KeyValuePair<string, long>> items)
    {
        var result = new PieChartViewModel
        {
            Items = new List<PieChartItem>()
        };

        var total = items.Sum(x => x.Value);
        foreach (var item in items)
        {
            int share = (int)(total / item.Value);
            //result.Items.Add(new PieChartItem { Name = item.Key, Value = share });
            result.Items.Add(new PieChartItem { Name = item.Key, Value = item.Value});
        }

        return result;
    }

    private PieChartViewModel GetRandomPieChart(params string[] valuesNames)
    {
        var result = new PieChartViewModel
        {
            Items = new List<PieChartItem>()
        };

        int total = 100;
        foreach (var name in valuesNames)
        {
            var val = Random.Shared.Next(10, 20);
            result.Items.Add(new PieChartItem { Name = name, Value = val });
            total -= val;
        }
        result.Items.Last().Value += total;

        return result;
    }

    private LineChartViewModel GetRandomTimeSeries(params string[] seriesNames)
    {
        var result = new LineChartViewModel()
        {
            Series = new List<TimeSeriesViewModel>()
        };

        foreach (var seriesName in seriesNames)
        {
            var vals = new List<TimeSeriesValuePoint>();
            for (int i = -30; i < 0; i++)
            {
                vals.Add(new TimeSeriesValuePoint() { TimePoint = DateTimeOffset.UtcNow.Date.AddDays(i), Value = Random.Shared.Next(50, 100) });
            }

            result.Series.Add(new TimeSeriesViewModel() { Name = seriesName, Values = vals });
        }

        return result;
    }
}
