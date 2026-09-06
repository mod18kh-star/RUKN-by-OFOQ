using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IPaymentIntentRepository
{
    Task<PaymentIntent?> GetByIdAsync(
        PaymentIntentId paymentIntentId,
        CancellationToken cancellationToken = default);

    Task<PaymentIntent?> GetByCreateIdempotencyKeyAsync(
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentIntent?> GetByProviderReferenceAsync(
        TenantPaymentMethodId tenantPaymentMethodId,
        string providerReference,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PaymentIntent paymentIntent,
        string createIdempotencyKey,
        CancellationToken cancellationToken = default);
}
