using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.State;

public sealed record SetPaymentProviderAccountStateCommand(
    TenantPaymentProviderAccountId AccountId,
    bool Enabled,
    Guid ActorUserId);