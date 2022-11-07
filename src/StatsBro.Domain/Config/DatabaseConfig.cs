namespace StatsBro.Domain.Config
{
    public class DatabaseConfig
    {
        public string DataSource { get; set; } = null!;

        public string? Password { get; set; }

        public bool Pooling { get; set; } = true;

        public int Version { get; set; } = 3;
    }
}
