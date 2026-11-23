namespace Orders.Infrastructure.Persistence;

internal sealed class ProcessedMessage
{
    public Guid EventId { get; init; }

    public required string Consumer { get; init; }

    public DateTimeOffset ProcessedAt { get; init; }
}
