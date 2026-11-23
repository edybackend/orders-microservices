using Orders.Domain;

namespace Orders.UnitTests;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Order NewOrder() =>
        Order.Place(Guid.NewGuid(), "ana@example.com", [new OrderItem("Keyboard", 2, 149.90m), new OrderItem("Mouse", 1, 50m)], Now);

    [Fact]
    public void Place_computes_total_and_starts_pending()
    {
        var order = NewOrder();

        Assert.Equal(349.80m, order.Total);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(2, order.Items.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Place_rejects_invalid_email(string email) =>
        Assert.Throws<DomainException>(() => Order.Place(Guid.NewGuid(), email, [new OrderItem("Keyboard", 1, 10m)], Now));

    [Fact]
    public void Place_requires_at_least_one_item() =>
        Assert.Throws<DomainException>(() => Order.Place(Guid.NewGuid(), "ana@example.com", [], Now));

    [Theory]
    [InlineData("", 1, 10)]
    [InlineData("Keyboard", 0, 10)]
    [InlineData("Keyboard", 1, 0)]
    public void Item_rejects_invalid_values(string name, int quantity, double unitPrice) =>
        Assert.Throws<DomainException>(() => new OrderItem(name, quantity, (decimal)unitPrice));

    [Fact]
    public void Pending_order_can_be_paid()
    {
        var order = NewOrder();
        var later = Now.AddSeconds(2);

        order.MarkAsPaid(later);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(later, order.UpdatedAt);
    }

    [Fact]
    public void Pending_order_can_be_rejected_with_reason()
    {
        var order = NewOrder();

        order.Reject("Limit exceeded", Now);

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal("Limit exceeded", order.RejectionReason);
    }

    [Fact]
    public void Paid_order_cannot_change_again()
    {
        var order = NewOrder();
        order.MarkAsPaid(Now);

        Assert.Throws<DomainException>(() => order.MarkAsPaid(Now));
        Assert.Throws<DomainException>(() => order.Reject("late", Now));
    }

    [Fact]
    public void Reject_requires_reason() =>
        Assert.Throws<DomainException>(() => NewOrder().Reject(" ", Now));
}
