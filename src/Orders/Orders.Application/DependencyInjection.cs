using Ecommerce.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.PaymentResults;
using Orders.Application.PlaceOrder;

namespace Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<PlaceOrderHandler>();
        services.AddScoped<PaymentResultConsumers>();
        services.AddScoped<IIntegrationEventConsumer<PaymentApproved>>(sp => sp.GetRequiredService<PaymentResultConsumers>());
        services.AddScoped<IIntegrationEventConsumer<PaymentRejected>>(sp => sp.GetRequiredService<PaymentResultConsumers>());
        return services;
    }
}
