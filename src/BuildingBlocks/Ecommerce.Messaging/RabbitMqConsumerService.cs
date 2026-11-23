using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ecommerce.Messaging;

/// <summary>
/// Consumes one queue with manual acknowledgements:
/// ack only after the handler succeeded; on failure the message is rejected (no requeue) and goes to the DLQ,
/// so a poison message cannot block the queue.
/// </summary>
internal sealed partial class RabbitMqConsumerService(
    Subscription subscription,
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerService> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(null, stoppingToken);
        await RabbitMqTopology.DeclareQueueAsync(_channel, _options, subscription.QueueName, subscription.RoutingKeys, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, _options.PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(_channel, delivery, stoppingToken);
        await _channel.BasicConsumeAsync(subscription.QueueName, autoAck: false, consumer, stoppingToken);
        LogListening(subscription.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        var messageType = delivery.BasicProperties.Type;
        if (!subscription.TryGetDispatcher(messageType, out var dispatch))
        {
            LogUnknownType(messageType, subscription.QueueName);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, ct);
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await dispatch(scope.ServiceProvider, delivery.Body, ct);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
        }
#pragma warning disable CA1031 // any handler failure must dead-letter the message instead of crashing the consumer
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogHandlerFailed(ex, messageType, delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, ct);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Listening on queue {Queue}")]
    private partial void LogListening(string queue);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown message type {Type} on {Queue}; sent to DLQ")]
    private partial void LogUnknownType(string? type, string queue);

    [LoggerMessage(Level = LogLevel.Error, Message = "Handler failed for {Type} (message {MessageId}); sent to DLQ")]
    private partial void LogHandlerFailed(Exception ex, string? type, string? messageId);
}
