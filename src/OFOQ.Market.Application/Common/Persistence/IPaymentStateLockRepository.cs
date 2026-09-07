using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IPaymentStateLockRepository
{
    Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetPaymentForUpdateAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentIntent?> GetIntentForUpdateAsync(
        PaymentIntentId paymentIntentId,
        CancellationToken cancellationToken = default);
}
