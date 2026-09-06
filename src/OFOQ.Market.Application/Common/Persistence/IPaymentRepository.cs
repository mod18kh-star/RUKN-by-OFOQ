using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default);
}
