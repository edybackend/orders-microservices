using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Ecommerce.Contracts;

namespace Ecommerce.Messaging;

/// <summary>
/// Publishes persistent messages with publisher confirms: <see cref="PublishAsync{TEvent}"/> only completes
/// after the broker has accepted the message, and throws if it was not routed or was rejected.
/// </summary>
public sealed class RabbitMqPublisher(RabbitMqConnection connection, IOptions<RabbitMqOptions> options)
    : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1); // a channel must not be used concurrently
    private IChannel? _channel;

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, string routingKey, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, MessagingJson.Options);
        var properties = new BasicProperties
        {
            MessageId = integrationEvent.EventId.ToString(),
            Type = typeof(TEvent).Name,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(integrationEvent.OccurredAt.ToUnixTimeSeconds()),
        };

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _channel ??= await connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                cancellationToken);

            await _channel.BasicPublishAsync(_options.ExchangeName, routingKey, mandatory: true, properties, body, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        _lock.Dispose();
    }
}
