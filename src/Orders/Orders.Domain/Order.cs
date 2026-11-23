using System.Net.Mail;

namespace Orders.Domain;

/// <summary>
/// Aggregate root. All state changes go through methods that enforce the lifecycle:
/// Pending → Paid, or Pending → Rejected. Nothing else is allowed.
/// </summary>
public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
        CustomerEmail = string.Empty; // required by EF Core
    }

    private Order(Guid id, string customerEmail, List<OrderItem> items, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerEmail = customerEmail;
        _items = items;
        Total = items.Sum(item => item.Subtotal);
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string CustomerEmail { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal Total { get; private set; }

    public OrderStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Order Place(Guid id, string customerEmail, IEnumerable<OrderItem> items, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (!MailAddress.TryCreate(customerEmail, out _))
        {
            throw new DomainException("Customer e-mail is not valid.");
        }

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new DomainException("An order must have at least one item.");
        }

        return new Order(id, customerEmail.Trim(), itemList, now);
    }

    public void MarkAsPaid(DateTimeOffset now)
    {
        EnsurePending(nameof(MarkAsPaid));
        Status = OrderStatus.Paid;
        UpdatedAt = now;
    }

    public void Reject(string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A rejection reason is required.");
        }

        EnsurePending(nameof(Reject));
        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = now;
    }

    private void EnsurePending(string operation)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException($"Cannot {operation} an order that is {Status}.");
        }
    }
}
