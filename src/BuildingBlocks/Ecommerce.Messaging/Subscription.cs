using System.Text.Json;
using Ecommerce.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Messaging;

/// <summary>
/// Describes one queue and the event types it accepts. Dispatch delegates are built here, with generics,
/// so no reflection is needed when a message arrives.
/// </summary>
public sealed class Subscription(string queueName)
{
    private readonly Dictionary<string, Func<IServiceProvider, ReadOnlyMemory<byte>, CancellationToken, Task>> _dispatchers = [];
    private readonly List<string> _routingKeys = [];

    public string QueueName { get; } = queueName;

    public IReadOnlyList<string> RoutingKeys => _routingKeys;

    public Subscription On<TEvent>(string routingKey)
        where TEvent : IIntegrationEvent
    {
        _routingKeys.Add(routingKey);
        _dispatchers[typeof(TEvent).Name] = (services, body, ct) =>
        {
            var integrationEvent = JsonSerializer.Deserialize<TEvent>(body.Span, MessagingJson.Options)
                ?? throw new JsonException($"Empty payload for {typeof(TEvent).Name}.");
            return services.GetRequiredService<IIntegrationEventConsumer<TEvent>>().ConsumeAsync(integrationEvent, ct);
        };
        return this;
    }

    internal bool TryGetDispatcher(
        string? messageType,
        out Func<IServiceProvider, ReadOnlyMemory<byte>, CancellationToken, Task> dispatcher) =>
        _dispatchers.TryGetValue(messageType ?? string.Empty, out dispatcher!);
}
