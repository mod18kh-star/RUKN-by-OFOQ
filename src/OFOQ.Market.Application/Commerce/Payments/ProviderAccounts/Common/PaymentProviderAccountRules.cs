using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;

internal static class PaymentProviderAccountRules
{
    public static TenantId GetRequiredTenantId(
        ICurrentTenant currentTenant)
    {
        ArgumentNullException.ThrowIfNull(
            currentTenant);

        if (!currentTenant.TenantId.HasValue ||
            currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }

        return currentTenant.TenantId.Value;
    }

    public static void EnsureOwned(
        TenantPaymentProviderAccount? account,
        TenantId tenantId)
    {
        if (account is null ||
            account.TenantId != tenantId)
        {
            throw new PaymentProviderAccountNotFoundException();
        }
    }

    public static PaymentProviderAccountResult MapAccount(
        TenantPaymentProviderAccount account,
        IEnumerable<TenantPaymentWalletCapability> wallets)
    {
        var walletResults = wallets
            .OrderBy(wallet => wallet.WalletType)
            .Select(MapWallet)
            .ToArray();

        return new PaymentProviderAccountResult(
            account.Id.Value,
            account.ProviderCode.Value,
            account.DisplayName,
            account.Environment.ToString(),
            account.IsEnabled,
            account.HasCredentials,
            account.CredentialsVersion,
            account.CreatedAtUtc,
            walletResults);
    }

    public static PaymentWalletCapabilityResult MapWallet(
        TenantPaymentWalletCapability wallet)
    {
        return new PaymentWalletCapabilityResult(
            wallet.Id.Value,
            wallet.WalletType.ToString(),
            wallet.IsEnabled);
    }
}