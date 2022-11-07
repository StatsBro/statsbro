namespace StatsBro.Services;

public class RabbitMQConfig
{
    public string Host { get; set; } = null!;

    public int Port { get; set; } = 5672;

    public string User { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string QueueName { get; set; } = "events";
}
