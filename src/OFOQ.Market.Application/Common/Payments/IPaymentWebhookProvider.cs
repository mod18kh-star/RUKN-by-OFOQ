using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Payments;

public interface IPaymentWebhookProvider
{
    PaymentProviderCode ProviderCode { get; }

    Task<PaymentWebhookResult> ParseAndValidateWebhookAsync(
        PaymentWebhookRequest request,
        CancellationToken cancellationToken = default);
}
