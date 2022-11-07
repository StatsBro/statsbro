namespace StatsBro.Panel.Models
{
    public class PieChartViewModel
    {
        public List<PieChartItem> Items { get; set; } = null!;

        public string SeriesToJSString()
        {
            var vals = string.Join(", ", Items.Select(v => v.Value));
            return $"[{vals}]";
        }

        public string LabelsToJSString()
        {
            var vals = string.Join(", ", Items.Select(v => $"'{v.Name}'"));
            return $"[{vals}]";
        }
    }
}
