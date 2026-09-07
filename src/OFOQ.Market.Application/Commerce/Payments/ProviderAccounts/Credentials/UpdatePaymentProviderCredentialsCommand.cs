using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Credentials;

public sealed record UpdatePaymentProviderCredentialsCommand(
    TenantPaymentProviderAccountId AccountId,
    IReadOnlyDictionary<string, string> Credentials,
    Guid ActorUserId);