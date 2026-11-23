using Ecommerce.Contracts;
using Ecommerce.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Abstractions;
using Orders.Application.Queries;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Queries;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public const string PaymentResultsQueue = "orders.payment-results";

    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Orders") ?? "Data Source=data/orders.db";

        services.AddDbContext<OrdersDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProcessedMessageStore, ProcessedMessageStore>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddSingleton(new SqliteConnectionFactory(connectionString));
        services.AddScoped<IOrderQueries, DapperOrderQueries>();

        services.AddRabbitMq(configuration);
        services.AddRabbitMqConsumer(PaymentResultsQueue, subscription => subscription
            .On<PaymentApproved>(RoutingKeys.PaymentApproved)
            .On<PaymentRejected>(RoutingKeys.PaymentRejected));

        return services;
    }
}
