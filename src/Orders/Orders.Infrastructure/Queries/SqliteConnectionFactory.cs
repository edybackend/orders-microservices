using Microsoft.Data.Sqlite;

namespace Orders.Infrastructure.Queries;

internal sealed class SqliteConnectionFactory(string connectionString)
{
    public SqliteConnection Create() => new(connectionString);
}
