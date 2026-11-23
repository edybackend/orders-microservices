using Ecommerce.Contracts;
using Orders.Application.Abstractions;
using Orders.Domain;

namespace Orders.Application.PlaceOrder;

public sealed class PlaceOrderHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher,
    TimeProvider clock)
{
    public async Task<Result<Guid>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Order order;
        try
        {
            var items = (command.Items ?? []).Select(i => new OrderItem(i.ProductName, i.Quantity, i.UnitPrice));
            order = Order.Place(Guid.CreateVersion7(), command.CustomerEmail, items, clock.GetUtcNow());
        }
        catch (DomainException ex)
        {
            return Result.Invalid<Guid>("order", ex.Message);
        }

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Known trade-off: the save and the publish are two separate operations (dual write).
        // If the broker is down right here, the order stays Pending. The Outbox pattern removes this gap.
        await publisher.PublishAsync(
            new OrderPlaced(Guid.CreateVersion7(), clock.GetUtcNow(), order.Id, order.CustomerEmail, order.Total),
            RoutingKeys.OrderPlaced,
            cancellationToken);

        return Result.Success(order.Id);
    }
}
