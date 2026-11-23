using Ecommerce.Contracts;
using Orders.Application.Abstractions;
using Orders.Domain;

namespace Orders.UnitTests;

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class InMemoryOrders : IOrderRepository, IUnitOfWork, IProcessedMessageStore
{
    private readonly HashSet<Guid> _processed = [];

    public Dictionary<Guid, Order> Stored { get; } = [];

    public int SaveCount { get; private set; }

    public void Add(Order order) => Stored[order.Id] = order;

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Stored.GetValueOrDefault(orderId));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken) =>
        Task.FromResult(_processed.Contains(eventId));

    public void MarkAsProcessed(Guid eventId, string consumer, DateTimeOffset processedAt) => _processed.Add(eventId);
}

internal sealed class RecordingPublisher : IEventPublisher
{
    public List<(IIntegrationEvent Event, string RoutingKey)> Published { get; } = [];

    public Task PublishAsync<TEvent>(TEvent integrationEvent, string routingKey, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        Published.Add((integrationEvent, routingKey));
        return Task.CompletedTask;
    }
}

internal sealed class RecordingNotifier : IOrderStatusNotifier
{
    public List<OrderStatusChanged> Changes { get; } = [];

    public ValueTask NotifyAsync(OrderStatusChanged change, CancellationToken cancellationToken)
    {
        Changes.Add(change);
        return ValueTask.CompletedTask;
    }
}
