namespace Ecommerce.Contracts;

/// <summary>
/// Marker for messages that cross service boundaries.
/// Integration events are immutable facts expressed in past tense.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
