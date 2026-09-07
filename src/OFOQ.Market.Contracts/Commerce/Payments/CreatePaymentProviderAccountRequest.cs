namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record CreatePaymentProviderAccountRequest(
    string ProviderCode,
    string DisplayName,
    string Environment);