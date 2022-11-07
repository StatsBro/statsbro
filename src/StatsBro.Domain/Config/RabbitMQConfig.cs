namespace StatsBro.Domain.Config
{
    public class RabbitMQConfig
    {
        public string Host { get; set; } = null!;

        public int Port { get; set; } = 5672;

        public string User { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string QueueName { get; set; } = "events";

        public string ExchangeNameConfigReload { get; set; } = "config_reload";

        public uint PrefetchedSize { get; set; } = 0;

        public ushort PrefetchedCount { get; set; } = 1;

        public bool QosIsGlobal { get; set; } = false;
    }
}