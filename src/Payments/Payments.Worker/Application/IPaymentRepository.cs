using Payments.Worker.Domain;

namespace Payments.Worker.Application;

public interface IPaymentRepository
{
    Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Inserts the payment unless the order was already charged. Returns false on a duplicate.</summary>
    Task<bool> TryAddAsync(Payment payment, CancellationToken cancellationToken);
}
