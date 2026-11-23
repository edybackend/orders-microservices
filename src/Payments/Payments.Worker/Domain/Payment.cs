namespace Payments.Worker.Domain;

public enum PaymentStatus
{
    Approved = 1,
    Rejected = 2,
}

/// <summary>
/// Result of charging one order. <see cref="ResultEventId"/> is generated once and stored,
/// so re-publishing the outcome always carries the same event id and consumers can deduplicate it.
/// </summary>
public sealed record Payment(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    PaymentStatus Status,
    string? RejectionReason,
    Guid ResultEventId,
    DateTimeOffset ProcessedAt)
{
    public static Payment Process(Guid orderId, decimal amount, PaymentPolicy policy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var decision = policy.Evaluate(amount);
        return new Payment(
            Guid.CreateVersion7(),
            orderId,
            amount,
            decision.Approved ? PaymentStatus.Approved : PaymentStatus.Rejected,
            decision.Reason,
            Guid.CreateVersion7(),
            now);
    }
}
