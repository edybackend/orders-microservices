using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Orders.Application.Queries;

namespace Orders.Infrastructure.Queries;

/// <summary>Read side: hand-written SQL with Dapper, no change tracking, only the columns the screen needs.</summary>
internal sealed class DapperOrderQueries(SqliteConnectionFactory connections) : IOrderQueries
{
    private const string SummaryColumns =
        "id AS Id, customer_email AS CustomerEmail, total AS Total, status AS Status, " +
        "rejection_reason AS RejectionReason, created_at AS CreatedAt";

    public async Task<IReadOnlyList<OrderSummary>> ListRecentAsync(int limit, CancellationToken cancellationToken)
    {
        await using var connection = connections.Create();
        var rows = await connection.QueryAsync<OrderRow>(new CommandDefinition(
            $"SELECT {SummaryColumns} FROM orders ORDER BY created_at DESC LIMIT @limit",
            new { limit },
            cancellationToken: cancellationToken));
        return [.. rows.Select(r => r.ToSummary())];
    }

    public async Task<OrderDetails?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await using var connection = connections.Create();
        var order = await connection.QuerySingleOrDefaultAsync<OrderRow>(new CommandDefinition(
            $"SELECT {SummaryColumns} FROM orders WHERE id = @orderId",
            new { orderId },
            cancellationToken: cancellationToken));
        if (order is null)
        {
            return null;
        }

        var lines = await connection.QueryAsync<LineRow>(new CommandDefinition(
            "SELECT product_name AS ProductName, quantity AS Quantity, unit_price AS UnitPrice " +
            "FROM order_items WHERE order_id = @orderId ORDER BY id",
            new { orderId },
            cancellationToken: cancellationToken));

        var summary = order.ToSummary();
        return new OrderDetails(
            summary.Id, summary.CustomerEmail, summary.Total, summary.Status, summary.RejectionReason, summary.CreatedAt,
            [.. lines.Select(l => new OrderLine(l.ProductName, (int)l.Quantity, ParseDecimal(l.UnitPrice)))]);
    }

    // SQLite has no decimal or date types: EF Core stores them as TEXT, so the read side parses them explicitly.
    private static decimal ParseDecimal(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private sealed record OrderRow(string Id, string CustomerEmail, string Total, string Status, string? RejectionReason, string CreatedAt)
    {
        public OrderSummary ToSummary() => new(
            Guid.Parse(Id),
            CustomerEmail,
            ParseDecimal(Total),
            Status,
            RejectionReason,
            DateTimeOffset.Parse(CreatedAt, CultureInfo.InvariantCulture));
    }

    private sealed record LineRow(string ProductName, long Quantity, string UnitPrice);
}
