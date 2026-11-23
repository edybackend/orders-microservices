namespace Ecommerce.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";

    public string ExchangeName { get; set; } = "ecommerce.events";

    public string DeadLetterExchangeName { get; set; } = "ecommerce.dlx";

    public ushort PrefetchCount { get; set; } = 10;

    public int ConnectRetryCount { get; set; } = 10;

    public TimeSpan ConnectRetryDelay { get; set; } = TimeSpan.FromSeconds(3);
}
