using Ecommerce.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Orders.IntegrationTests;

/// <summary>
/// Runs the real API with a throw-away SQLite file. RabbitMQ is replaced by an in-memory publisher
/// and the consumers are not started, so the tests need no broker.
/// </summary>
public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"orders-tests-{Guid.NewGuid():N}.db");

    public RecordingPublisher Publisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", $"Data Source={_databasePath};Pooling=False");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(Publisher);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        File.Delete(_databasePath);
    }
}

public sealed class RecordingPublisher : IEventPublisher
{
    private readonly List<IIntegrationEvent> _published = [];

    public IReadOnlyList<IIntegrationEvent> Published
    {
        get
        {
            lock (_published)
            {
                return [.. _published];
            }
        }
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, string routingKey, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        lock (_published)
        {
            _published.Add(integrationEvent);
        }

        return Task.CompletedTask;
    }
}
