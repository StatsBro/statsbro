using System.Data;

namespace StatsBro.Panel.Models
{
    public class TableViewModel : WithQueryViewModel
    {
        public DataTable Data { get; set; } = null!;
    }
}
