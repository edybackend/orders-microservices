using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Creates the SQLite folder if needed and applies pending EF Core migrations.</summary>
    public static async Task MigrateOrdersDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var dataSource = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory) && dataSource != ":memory:")
        {
            Directory.CreateDirectory(directory);
        }

        await db.Database.MigrateAsync();
    }
}
