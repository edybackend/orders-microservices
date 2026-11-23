namespace Ecommerce.Contracts;

/// <summary>Publishes integration events. Implemented by the messaging infrastructure.</summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, string routingKey, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;
}

/// <summary>Consumes one integration event type. Implementations must be idempotent.</summary>
public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task ConsumeAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
