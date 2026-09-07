using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantPaymentProviderAccountStore
{
    public object SyncRoot { get; } =
        new();

    public List<TenantPaymentProviderAccount> Items { get; } =
        [];
}

internal sealed class InMemoryTenantPaymentProviderAccountRepository :
    ITenantPaymentProviderAccountRepository
{
    private readonly InMemoryTenantPaymentProviderAccountStore _store;
    private readonly ICurrentTenant _currentTenant;

    public InMemoryTenantPaymentProviderAccountRepository(
        InMemoryTenantPaymentProviderAccountStore store,
        ICurrentTenant currentTenant)
    {
        _store = store;
        _currentTenant = currentTenant;
    }

    public Task<TenantPaymentProviderAccount?> GetByIdAsync(
        TenantPaymentProviderAccountId accountId,
        CancellationToken cancellationToken = default)
    {
        if (accountId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider account ID cannot be empty.",
                nameof(accountId));
        }

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var account =
                _store.Items.SingleOrDefault(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == accountId);

            return Task.FromResult(
                account);
        }
    }

    public Task<TenantPaymentProviderAccount?> GetByProviderAndEnvironmentAsync(
        PaymentProviderCode providerCode,
        PaymentProviderEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (providerCode.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider code is required.",
                nameof(providerCode));
        }

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var account =
                _store.Items.SingleOrDefault(
                    item =>
                        item.TenantId == tenantId &&
                        item.ProviderCode == providerCode &&
                        item.Environment == environment);

            return Task.FromResult(
                account);
        }
    }

    public Task<TenantPaymentProviderAccount?> GetEnabledByProviderAsync(
        PaymentProviderCode providerCode,
        CancellationToken cancellationToken = default)
    {
        if (providerCode.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider code is required.",
                nameof(providerCode));
        }

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var account =
                _store.Items.SingleOrDefault(
                    item =>
                        item.TenantId == tenantId &&
                        item.ProviderCode == providerCode &&
                        item.IsEnabled);

            return Task.FromResult(
                account);
        }
    }

    public Task<IReadOnlyList<TenantPaymentProviderAccount>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<TenantPaymentProviderAccount> accounts =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId == tenantId)
                    .OrderBy(
                        item =>
                            item.CreatedAtUtc)
                    .ToArray();

            return Task.FromResult(
                accounts);
        }
    }

    public Task AddAsync(
        TenantPaymentProviderAccount account,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            account);

        var tenantId =
            GetRequiredTenantId();

        if (account.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant payment provider account creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                account);
        }

        return Task.CompletedTask;
    }

    private TenantId GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "An active tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}