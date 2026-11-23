using System.Globalization;

namespace Payments.Worker.Domain;

public sealed record PaymentDecision(bool Approved, string? Reason);

/// <summary>Business rule of the demo: amounts above the approval limit are rejected.</summary>
public sealed class PaymentPolicy(decimal approvalLimit)
{
    public PaymentDecision Evaluate(decimal amount)
    {
        if (amount <= 0)
        {
            return new PaymentDecision(false, "Amount must be greater than zero.");
        }

        return amount <= approvalLimit
            ? new PaymentDecision(true, null)
            : new PaymentDecision(false, string.Create(CultureInfo.InvariantCulture, $"Amount exceeds the approval limit of {approvalLimit:F2}."));
    }
}
