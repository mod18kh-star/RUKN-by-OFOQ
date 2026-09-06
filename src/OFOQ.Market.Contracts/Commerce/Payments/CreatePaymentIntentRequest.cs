namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record CreatePaymentIntentRequest(
    Guid TenantPaymentMethodId);
