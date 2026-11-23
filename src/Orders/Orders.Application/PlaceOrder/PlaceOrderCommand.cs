namespace Orders.Application.PlaceOrder;

public sealed record PlaceOrderCommand(string CustomerEmail, IReadOnlyList<PlaceOrderItem> Items);

public sealed record PlaceOrderItem(string ProductName, int Quantity, decimal UnitPrice);
