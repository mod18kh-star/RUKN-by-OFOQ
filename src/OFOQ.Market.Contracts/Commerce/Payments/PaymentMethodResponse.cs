namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record PaymentMethodResponse(
    Guid Id,
    string MethodType,
    string ProviderCode,
    string DisplayName,
    string Country,
    string Currency,
    decimal? MinimumAmount,
    decimal? MaximumAmount);
