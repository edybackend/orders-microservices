namespace Payments.Worker.Application;

public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    public decimal ApprovalLimit { get; set; } = 5000m;

    /// <summary>Simulates the latency of a real payment gateway, so the UI shows the Pending state.</summary>
    public TimeSpan SimulatedLatency { get; set; } = TimeSpan.FromSeconds(2);
}
