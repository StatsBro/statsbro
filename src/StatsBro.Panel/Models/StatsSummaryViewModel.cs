namespace StatsBro.Panel.Models
{
    public class StatsSummaryViewModel
    {
        public long TotalUsersCount { get; set; }
        public int? TotalUsersChange { get; set; }
        public int TotalEngagedUsersCount { get; set; }
        public int? TotalEngagedUsersChange { get; set; }
        public long TotalPageViews { get; set; }
        public int? TotalPageViewChange { get; set; }
        public TimeSpan AverageVisitLength { get; set; }
        public int? AverageVisitLengthChange { get; set; }
    }
}
