using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Domain;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    public void Add(Order order) => db.Orders.Add(order);

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
}

internal sealed class ProcessedMessageStore(OrdersDbContext db) : IProcessedMessageStore
{
    public Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken) =>
        db.ProcessedMessages.AnyAsync(m => m.EventId == eventId, cancellationToken);

    public void MarkAsProcessed(Guid eventId, string consumer, DateTimeOffset processedAt) =>
        db.ProcessedMessages.Add(new ProcessedMessage { EventId = eventId, Consumer = consumer, ProcessedAt = processedAt });
}

internal sealed class EfUnitOfWork(OrdersDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
