using Ecommerce.Contracts;
using Orders.Application.PaymentResults;
using Orders.Domain;

namespace Orders.UnitTests;

public class PaymentResultConsumersTests
{
    private readonly InMemoryOrders _orders = new();
    private readonly RecordingNotifier _notifier = new();
    private readonly PaymentResultConsumers _consumers;
    private readonly Order _order;

    public PaymentResultConsumersTests()
    {
        _consumers = new PaymentResultConsumers(_orders, _orders, _orders, _notifier, new FixedClock(DateTimeOffset.UnixEpoch));
        _order = Order.Place(Guid.NewGuid(), "ana@example.com", [new OrderItem("Keyboard", 1, 100m)], DateTimeOffset.UnixEpoch);
        _orders.Add(_order);
    }

    [Fact]
    public async Task Approved_payment_marks_order_as_paid_and_notifies()
    {
        await _consumers.ConsumeAsync(Approved(Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(OrderStatus.Paid, _order.Status);
        Assert.Equal(OrderStatus.Paid, Assert.Single(_notifier.Changes).Status);
    }

    [Fact]
    public async Task Rejected_payment_stores_the_reason()
    {
        var rejected = new PaymentRejected(Guid.NewGuid(), DateTimeOffset.UnixEpoch, _order.Id, "Limit exceeded");

        await _consumers.ConsumeAsync(rejected, TestContext.Current.CancellationToken);

        Assert.Equal(OrderStatus.Rejected, _order.Status);
        Assert.Equal("Limit exceeded", _order.RejectionReason);
    }

    [Fact]
    public async Task Redelivered_event_is_applied_only_once()
    {
        var approved = Approved(Guid.NewGuid());

        await _consumers.ConsumeAsync(approved, TestContext.Current.CancellationToken);
        await _consumers.ConsumeAsync(approved, TestContext.Current.CancellationToken); // would throw if applied twice

        Assert.Equal(1, _orders.SaveCount);
        Assert.Single(_notifier.Changes);
    }

    [Fact]
    public async Task Unknown_order_fails_so_the_message_is_dead_lettered()
    {
        var approved = new PaymentApproved(Guid.NewGuid(), DateTimeOffset.UnixEpoch, Guid.NewGuid(), Guid.NewGuid(), 10m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _consumers.ConsumeAsync(approved, TestContext.Current.CancellationToken));
    }

    private PaymentApproved Approved(Guid eventId) =>
        new(eventId, DateTimeOffset.UnixEpoch, _order.Id, Guid.NewGuid(), _order.Total);
}
