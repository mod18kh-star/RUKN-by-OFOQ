namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record SetPaymentWalletCapabilityRequest(
    string WalletType,
    bool Enabled);