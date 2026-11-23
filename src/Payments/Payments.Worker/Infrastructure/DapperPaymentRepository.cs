using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Payments.Worker.Application;
using Payments.Worker.Domain;

namespace Payments.Worker.Infrastructure;

/// <summary>
/// Dapper is enough here: one table, two statements, no aggregate graph to track.
/// Decimals and dates are stored as invariant TEXT because SQLite has no native types for them.
/// </summary>
internal sealed class DapperPaymentRepository(string connectionString) : IPaymentRepository
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS payments (
            id               TEXT NOT NULL PRIMARY KEY,
            order_id         TEXT NOT NULL UNIQUE,
            amount           TEXT NOT NULL,
            status           TEXT NOT NULL,
            rejection_reason TEXT NULL,
            result_event_id  TEXT NOT NULL,
            processed_at     TEXT NOT NULL
        );
        """;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory) && dataSource != ":memory:")
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = new SqliteConnection(connectionString);
        await connection.ExecuteAsync(new CommandDefinition(Schema, cancellationToken: cancellationToken));
    }

    public async Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(new CommandDefinition(
            "SELECT id AS Id, order_id AS OrderId, amount AS Amount, status AS Status, rejection_reason AS RejectionReason, " +
            "result_event_id AS ResultEventId, processed_at AS ProcessedAt FROM payments WHERE order_id = @orderId",
            new { orderId = orderId.ToString() },
            cancellationToken: cancellationToken));
        return row?.ToPayment();
    }

    public async Task<bool> TryAddAsync(Payment payment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);
        await using var connection = new SqliteConnection(connectionString);
        var inserted = await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO payments (id, order_id, amount, status, rejection_reason, result_event_id, processed_at) " +
            "VALUES (@Id, @OrderId, @Amount, @Status, @RejectionReason, @ResultEventId, @ProcessedAt) " +
            "ON CONFLICT(order_id) DO NOTHING",
            new
            {
                Id = payment.Id.ToString(),
                OrderId = payment.OrderId.ToString(),
                Amount = payment.Amount.ToString(CultureInfo.InvariantCulture),
                Status = payment.Status.ToString(),
                payment.RejectionReason,
                ResultEventId = payment.ResultEventId.ToString(),
                ProcessedAt = payment.ProcessedAt.ToString("O", CultureInfo.InvariantCulture),
            },
            cancellationToken: cancellationToken));
        return inserted == 1;
    }

    private sealed record PaymentRow(string Id, string OrderId, string Amount, string Status, string? RejectionReason, string ResultEventId, string ProcessedAt)
    {
        public Payment ToPayment() => new(
            Guid.Parse(Id),
            Guid.Parse(OrderId),
            decimal.Parse(Amount, CultureInfo.InvariantCulture),
            Enum.Parse<PaymentStatus>(Status),
            RejectionReason,
            Guid.Parse(ResultEventId),
            DateTimeOffset.Parse(ProcessedAt, CultureInfo.InvariantCulture));
    }
}
