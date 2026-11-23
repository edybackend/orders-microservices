using Payments.Worker.Domain;

namespace Payments.UnitTests;

public class PaymentPolicyTests
{
    private readonly PaymentPolicy _policy = new(approvalLimit: 5000m);

    [Theory]
    [InlineData(0.01, true)]
    [InlineData(5000.00, true)]
    [InlineData(5000.01, false)]
    [InlineData(0, false)]
    public void Evaluate_applies_the_approval_limit(double amount, bool expectedApproved)
    {
        var decision = _policy.Evaluate((decimal)amount);

        Assert.Equal(expectedApproved, decision.Approved);
        Assert.Equal(expectedApproved, decision.Reason is null);
    }
}
