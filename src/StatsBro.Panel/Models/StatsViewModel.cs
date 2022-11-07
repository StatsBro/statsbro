using StatsBro.Domain.Models;
using System.Data;

namespace StatsBro.Panel.Models
{
    public class StatsViewModel
    {
        public string Query { get; set; } = null!;
        public Site Site { get; set; } = null!;
        public StatsSummaryViewModel Summary { get; set; } = null!;

        public LineChartViewModel UsersInTime { get; set; } = null!;
        public LineChartViewModel PageViewsInTime { get; set; } = null!;
        public LineChartViewModel AverageVisitLengthInTime { get; set; } = null!;

        public PieChartViewModel TrafficSources { get; set; } = null!;
        public PieChartViewModel ScreenSizes { get; set; } = null!;
        public PieChartViewModel TouchScreens { get; set; } = null!;
        public PieChartViewModel Browsers { get; set; } = null!;
        public PieChartViewModel OSs { get; set; } = null!;

        public TableViewModel Campaigns { get; set; } = null!;
        public TableViewModel PageViews { get; set; } = null!;

        public TableViewModel EntryPages { get; set; } = null!;
        public TableViewModel PagesWithMostEngagement { get; set; } = null!;
        public TableViewModel Languages { get;  set; } = null!;
        public TableViewModel Cities { get; set; } = null!;
        public TableViewModel Countries { get; set; } = null!;

        public string GetChangeValueClass(int? change) => change >= 0 ? "text-success" : "text-danger";
        public string GetChangeValueText(int? change) => change >= 0 ? "wzrost" : "spadek";

    }
}
