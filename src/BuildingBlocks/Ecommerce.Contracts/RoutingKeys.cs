namespace Ecommerce.Contracts;

/// <summary>
/// Routing keys published to the <c>ecommerce.events</c> topic exchange.
/// Keep in sync with <c>contracts/events.md</c>, which the Go service follows.
/// </summary>
public static class RoutingKeys
{
    public const string OrderPlaced = "order.placed";
    public const string PaymentApproved = "payment.approved";
    public const string PaymentRejected = "payment.rejected";
    public const string AllPaymentResults = "payment.*";
}
