namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record PaymentWalletCapabilityResponse(
    Guid CapabilityId,
    string WalletType,
    bool IsEnabled);