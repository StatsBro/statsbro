namespace StatsBro.Panel.Models
{
    public class TimeSeriesViewModel
    {
        public List<TimeSeriesValuePoint> Values { get; set; } = null!;
        public string Name { get; set; } = null!;

        public string ValsToJSString()
        {
            var vals = string.Join(", ", Values.Select(v => v.Value));
            return $"{{ name: '{Name}', data: [{vals}] }},";
        }
    }
}
