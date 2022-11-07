namespace StatsBro.Panel.Models
{
    public class WithQueryViewModel
    {
        public string Query { get; set; } = null!;
        public string FieldName { get; set; } = null!;

        public string GetLink(string value)
        {
            if (string.IsNullOrEmpty(Query))
            {
                return $"?Query={FieldName}:({value})";
            }

            if (Query.Contains($"{FieldName}:"))
            {
                var fieldNamePos = Query.IndexOf($"{FieldName}:");
                var valStart = Query.IndexOf("(", fieldNamePos);
                var valEnd = Query.IndexOf(")", valStart);
                var val = Query.Substring(valStart + 1, valEnd - valStart - 1);
                return "?Query=" + Query.Replace($"{FieldName}:({val})", $"{FieldName}:({value})");
            }
            else
            {
                return $"?Query={Query} AND {FieldName}:({value})";
            }
        }
    }
}
