using Ecommerce.Contracts;
using Microsoft.Extensions.Options;
using Payments.Worker.Domain;

namespace Payments.Worker.Application;

/// <summary>
/// Charges an order exactly once. The unique order id in storage is the idempotency key:
/// a redelivered OrderPlaced finds the stored payment and re-publishes the same result event.
/// </summary>
public sealed class OrderPlacedConsumer(
    IPaymentRepository payments,
    IEventPublisher publisher,
    IOptions<PaymentOptions> options,
    TimeProvider clock) : IIntegrationEventConsumer<OrderPlaced>
{
    private readonly PaymentOptions _options = options.Value;

    public async Task ConsumeAsync(OrderPlaced integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payment = await payments.FindByOrderIdAsync(integrationEvent.OrderId, cancellationToken);
        if (payment is null)
        {
            if (_options.SimulatedLatency > TimeSpan.Zero)
            {
                await Task.Delay(_options.SimulatedLatency, clock, cancellationToken);
            }

            var candidate = Payment.Process(integrationEvent.OrderId, integrationEvent.Total, new PaymentPolicy(_options.ApprovalLimit), clock.GetUtcNow());
            payment = await payments.TryAddAsync(candidate, cancellationToken)
                ? candidate
                : await payments.FindByOrderIdAsync(integrationEvent.OrderId, cancellationToken)
                  ?? throw new InvalidOperationException($"Payment for order {integrationEvent.OrderId} vanished after a duplicate insert.");
        }

        await PublishResultAsync(payment, cancellationToken);
    }

    private Task PublishResultAsync(Payment payment, CancellationToken cancellationToken) =>
        payment.Status == PaymentStatus.Approved
            ? publisher.PublishAsync(
                new PaymentApproved(payment.ResultEventId, payment.ProcessedAt, payment.OrderId, payment.Id, payment.Amount),
                RoutingKeys.PaymentApproved,
                cancellationToken)
            : publisher.PublishAsync(
                new PaymentRejected(payment.ResultEventId, payment.ProcessedAt, payment.OrderId, payment.RejectionReason ?? "Rejected."),
                RoutingKeys.PaymentRejected,
                cancellationToken);
}
