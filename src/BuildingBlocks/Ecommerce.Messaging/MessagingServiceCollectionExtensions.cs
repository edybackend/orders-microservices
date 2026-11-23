using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ecommerce.Contracts;

namespace Ecommerce.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
        return services;
    }

    public static IServiceCollection AddRabbitMqConsumer(
        this IServiceCollection services,
        string queueName,
        Action<Subscription> configure)
    {
        var subscription = new Subscription(queueName);
        configure(subscription);
        services.AddSingleton<IHostedService>(sp => new RabbitMqConsumerService(
            subscription,
            sp.GetRequiredService<RabbitMqConnection>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOptions<RabbitMqOptions>>(),
            sp.GetRequiredService<ILogger<RabbitMqConsumerService>>()));
        return services;
    }
}
