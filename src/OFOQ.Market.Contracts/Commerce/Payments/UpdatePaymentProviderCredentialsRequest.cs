namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record UpdatePaymentProviderCredentialsRequest(
    IReadOnlyDictionary<string, string> Credentials);