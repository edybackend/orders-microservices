namespace Orders.Domain;

public sealed class OrderItem
{
    public const int MaxProductNameLength = 100;

    private OrderItem()
    {
        ProductName = string.Empty; // required by EF Core
    }

    public OrderItem(string productName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName) || productName.Length > MaxProductNameLength)
        {
            throw new DomainException($"Product name is required and must have at most {MaxProductNameLength} characters.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new DomainException("Unit price must be greater than zero.");
        }

        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string ProductName { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Subtotal => Quantity * UnitPrice;
}
