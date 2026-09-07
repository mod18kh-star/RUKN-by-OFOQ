using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantPaymentWalletCapabilityStore
{
    public object SyncRoot { get; } =
        new();

    public List<TenantPaymentWalletCapability> Items { get; } =
        [];
}

internal sealed class InMemoryTenantPaymentWalletCapabilityRepository :
    ITenantPaymentWalletCapabilityRepository
{
    private readonly InMemoryTenantPaymentWalletCapabilityStore _store;
    private readonly ICurrentTenant _currentTenant;

    public InMemoryTenantPaymentWalletCapabilityRepository(
        InMemoryTenantPaymentWalletCapabilityStore store,
        ICurrentTenant currentTenant)
    {
        _store = store;
        _currentTenant = currentTenant;
    }

    public Task<TenantPaymentWalletCapability?> GetByIdAsync(
        TenantPaymentWalletCapabilityId capabilityId,
        CancellationToken cancellationToken = default)
    {
        if (capabilityId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment wallet capability ID cannot be empty.",
                nameof(capabilityId));
        }

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var capability =
                _store.Items.SingleOrDefault(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == capabilityId);

            return Task.FromResult(
                capability);
        }
    }

    public Task<TenantPaymentWalletCapability?> GetByAccountAndWalletAsync(
        TenantPaymentProviderAccountId providerAccountId,
        PaymentWalletType walletType,
        CancellationToken cancellationToken = default)
    {
        if (providerAccountId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider account ID cannot be empty.",
                nameof(providerAccountId));
        }

        if (walletType == PaymentWalletType.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(walletType),
                "A supported payment wallet type is required.");
        }

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var capability =
                _store.Items.SingleOrDefault(
                    item =>
                        item.TenantId == tenantId &&
                        item.ProviderAccountId == providerAccountId &&
                        item.WalletType == walletType);

            return Task.FromResult(
                capability);
        }
    }

    public Task<IReadOnlyList<TenantPaymentWalletCapability>> GetByAccountAsync(
        TenantPaymentProviderAccountId providerAccountId,
        CancellationToken cancellationToken = default)
    {
        if (providerAccountId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider account ID cannot be empty.",
                nameof(providerAccountId));
        }

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<TenantPaymentWalletCapability> capabilities =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            item.ProviderAccountId ==
                            providerAccountId)
                    .OrderBy(
                        item =>
                            item.WalletType)
                    .ToArray();

            return Task.FromResult(
                capabilities);
        }
    }

    public Task AddAsync(
        TenantPaymentWalletCapability capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            capability);

        var tenantId =
            GetRequiredTenantId();

        if (capability.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant payment wallet capability creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                capability);
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