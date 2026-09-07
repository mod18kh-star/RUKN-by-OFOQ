using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Wallets;

public sealed record SetPaymentWalletCapabilityCommand(
    TenantPaymentProviderAccountId AccountId,
    PaymentWalletType WalletType,
    bool Enabled,
    Guid ActorUserId);