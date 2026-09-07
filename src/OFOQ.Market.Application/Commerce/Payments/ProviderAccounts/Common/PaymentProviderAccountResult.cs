namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;

public sealed record PaymentWalletCapabilityResult(
    Guid CapabilityId,
    string WalletType,
    bool IsEnabled);

public sealed record PaymentProviderAccountResult(
    Guid AccountId,
    string ProviderCode,
    string DisplayName,
    string Environment,
    bool IsEnabled,
    bool HasCredentials,
    int CredentialsVersion,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<PaymentWalletCapabilityResult> Wallets);