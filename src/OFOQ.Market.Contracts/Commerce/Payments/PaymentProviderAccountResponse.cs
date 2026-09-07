namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record PaymentProviderAccountResponse(
    Guid AccountId,
    string ProviderCode,
    string DisplayName,
    string Environment,
    bool IsEnabled,
    bool HasCredentials,
    int CredentialsVersion,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<PaymentWalletCapabilityResponse> Wallets);