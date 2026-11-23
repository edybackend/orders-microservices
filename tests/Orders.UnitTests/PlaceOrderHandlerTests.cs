using Ecommerce.Contracts;
using Orders.Application.PlaceOrder;

namespace Orders.UnitTests;

public class PlaceOrderHandlerTests
{
    private readonly InMemoryOrders _orders = new();
    private readonly RecordingPublisher _publisher = new();
    private readonly PlaceOrderHandler _handler;

    public PlaceOrderHandlerTests() =>
        _handler = new PlaceOrderHandler(_orders, _orders, _publisher, new FixedClock(DateTimeOffset.UnixEpoch));

    [Fact]
    public async Task Valid_command_saves_order_then_publishes_OrderPlaced()
    {
        var command = new PlaceOrderCommand("ana@example.com", [new PlaceOrderItem("Keyboard", 2, 149.90m)]);

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _orders.SaveCount);
        var (published, routingKey) = Assert.Single(_publisher.Published);
        var orderPlaced = Assert.IsType<OrderPlaced>(published);
        Assert.Equal(RoutingKeys.OrderPlaced, routingKey);
        Assert.Equal(result.Value, orderPlaced.OrderId);
        Assert.Equal(299.80m, orderPlaced.Total);
    }

    [Fact]
    public async Task Invalid_command_returns_errors_and_has_no_side_effects()
    {
        var command = new PlaceOrderCommand("not-an-email", [new PlaceOrderItem("Keyboard", 1, 10m)]);

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Errors);
        Assert.Equal(0, _orders.SaveCount);
        Assert.Empty(_publisher.Published);
    }
}
