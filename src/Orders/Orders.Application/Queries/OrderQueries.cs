namespace Orders.Application.Queries;

/// <summary>
/// Read side of CQRS: returns flat DTOs straight from SQL (implemented with Dapper),
/// without loading aggregates or tracking changes.
/// </summary>
public interface IOrderQueries
{
    Task<IReadOnlyList<OrderSummary>> ListRecentAsync(int limit, CancellationToken cancellationToken);

    Task<OrderDetails?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
}

public sealed record OrderSummary(
    Guid Id,
    string CustomerEmail,
    decimal Total,
    string Status,
    string? RejectionReason,
    DateTimeOffset CreatedAt);

public sealed record OrderDetails(
    Guid Id,
    string CustomerEmail,
    decimal Total,
    string Status,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLine> Items);

public sealed record OrderLine(string ProductName, int Quantity, decimal UnitPrice);
