using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Get;

public sealed class GetPaymentProviderAccountsHandler
{
    private readonly ITenantPaymentProviderAccountRepository _accountRepository;
    private readonly ITenantPaymentWalletCapabilityRepository _walletRepository;
    private readonly ICurrentTenant _currentTenant;

    public GetPaymentProviderAccountsHandler(
        ITenantPaymentProviderAccountRepository accountRepository,
        ITenantPaymentWalletCapabilityRepository walletRepository,
        ICurrentTenant currentTenant)
    {
        _accountRepository = accountRepository;
        _walletRepository = walletRepository;
        _currentTenant = currentTenant;
    }

    public async Task<IReadOnlyList<PaymentProviderAccountResult>> HandleAsync(
        GetPaymentProviderAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        var tenantId =
            PaymentProviderAccountRules.GetRequiredTenantId(
                _currentTenant);

        var accounts =
            await _accountRepository.GetAllAsync(
                cancellationToken);

        var results =
            new List<PaymentProviderAccountResult>();

        foreach (var account in accounts)
        {
            if (account.TenantId != tenantId)
            {
                continue;
            }

            var wallets =
                await _walletRepository.GetByAccountAsync(
                    account.Id,
                    cancellationToken);

            results.Add(
                PaymentProviderAccountRules.MapAccount(
                    account,
                    wallets));
        }

        return results
            .OrderBy(result => result.ProviderCode)
            .ThenBy(result => result.Environment)
            .ToArray();
    }
}