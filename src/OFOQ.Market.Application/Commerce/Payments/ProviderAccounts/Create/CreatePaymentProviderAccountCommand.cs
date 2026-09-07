using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Create;

public sealed record CreatePaymentProviderAccountCommand(
    string ProviderCode,
    string DisplayName,
    PaymentProviderEnvironment Environment,
    Guid ActorUserId);