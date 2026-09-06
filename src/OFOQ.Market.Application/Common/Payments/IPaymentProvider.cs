using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Payments;

public interface IPaymentProvider
{
    PaymentProviderCode ProviderCode { get; }

    Task<PaymentProviderResult> CreatePaymentAsync(
        PaymentProviderRequest request,
        CancellationToken cancellationToken = default);
}
