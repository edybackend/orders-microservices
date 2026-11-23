using Orders.Domain;

namespace Orders.Application.Abstractions;

/// <summary>Write-side persistence for the Order aggregate (implemented with EF Core).</summary>
public interface IOrderRepository
{
    void Add(Order order);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
}

/// <summary>Records consumed messages so a redelivered event is applied only once.</summary>
public interface IProcessedMessageStore
{
    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken);

    void MarkAsProcessed(Guid eventId, string consumer, DateTimeOffset processedAt);
}

/// <summary>Commits every change tracked in the current scope atomically.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Pushes status changes to connected clients (Server-Sent Events).</summary>
public interface IOrderStatusNotifier
{
    ValueTask NotifyAsync(OrderStatusChanged change, CancellationToken cancellationToken);
}

public sealed record OrderStatusChanged(Guid OrderId, OrderStatus Status, string? RejectionReason, DateTimeOffset ChangedAt);
