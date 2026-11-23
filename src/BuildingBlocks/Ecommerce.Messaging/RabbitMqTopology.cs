using RabbitMQ.Client;

namespace Ecommerce.Messaging;

/// <summary>
/// Declares exchanges and queues. Declarations are idempotent, so every service declares what it needs
/// instead of depending on a manual broker setup.
/// </summary>
internal static class RabbitMqTopology
{
    public static async Task DeclareExchangesAsync(IChannel channel, RabbitMqOptions options, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(options.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(options.DeadLetterExchangeName, ExchangeType.Direct, durable: true, cancellationToken: ct);
    }

    public static async Task DeclareQueueAsync(
        IChannel channel,
        RabbitMqOptions options,
        string queueName,
        IEnumerable<string> routingKeys,
        CancellationToken ct)
    {
        var deadLetterQueue = $"{queueName}.dlq";
        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(deadLetterQueue, options.DeadLetterExchangeName, deadLetterQueue, cancellationToken: ct);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = options.DeadLetterExchangeName,
            ["x-dead-letter-routing-key"] = deadLetterQueue,
        };
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments, cancellationToken: ct);

        foreach (var routingKey in routingKeys)
        {
            await channel.QueueBindAsync(queueName, options.ExchangeName, routingKey, cancellationToken: ct);
        }
    }
}
