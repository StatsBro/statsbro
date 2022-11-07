namespace StatsBro.Panel.Models
{
    public class LineChartViewModel
    {
        public List<TimeSeriesViewModel> Series { get; set; } = null!;

        public string TimePointsToJSString()
        {
            var vals = string.Join(", ", Series.First().Values.Select(v => $"'{v.TimePoint.ToString("u")}'"));
            return $"[{vals}]";
        }
    }
}
