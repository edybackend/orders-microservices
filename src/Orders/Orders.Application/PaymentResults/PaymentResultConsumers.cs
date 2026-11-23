using Ecommerce.Contracts;
using Orders.Application.Abstractions;
using Orders.Domain;

namespace Orders.Application.PaymentResults;

/// <summary>
/// Applies the payment outcome to the order. Idempotent: the event id is stored in the same
/// transaction as the status change, so a redelivered message is ignored.
/// </summary>
public sealed class PaymentResultConsumers(
    IOrderRepository orders,
    IProcessedMessageStore processedMessages,
    IUnitOfWork unitOfWork,
    IOrderStatusNotifier notifier,
    TimeProvider clock)
    : IIntegrationEventConsumer<PaymentApproved>, IIntegrationEventConsumer<PaymentRejected>
{
    private const string ConsumerName = "orders.payment-results";

    public Task ConsumeAsync(PaymentApproved integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return ApplyAsync(integrationEvent.EventId, integrationEvent.OrderId, (order, now) => order.MarkAsPaid(now), cancellationToken);
    }

    public Task ConsumeAsync(PaymentRejected integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return ApplyAsync(integrationEvent.EventId, integrationEvent.OrderId, (order, now) => order.Reject(integrationEvent.Reason, now), cancellationToken);
    }

    private async Task ApplyAsync(Guid eventId, Guid orderId, Action<Order, DateTimeOffset> apply, CancellationToken cancellationToken)
    {
        if (await processedMessages.HasBeenProcessedAsync(eventId, cancellationToken))
        {
            return;
        }

        var order = await orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        var now = clock.GetUtcNow();
        apply(order, now);
        processedMessages.MarkAsProcessed(eventId, ConsumerName, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.NotifyAsync(new OrderStatusChanged(order.Id, order.Status, order.RejectionReason, now), cancellationToken);
    }
}
