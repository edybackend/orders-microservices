namespace Ecommerce.Contracts;

public sealed record OrderPlaced(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string CustomerEmail,
    decimal Total) : IIntegrationEvent;

public sealed record PaymentApproved(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount) : IIntegrationEvent;

public sealed record PaymentRejected(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string Reason) : IIntegrationEvent;
