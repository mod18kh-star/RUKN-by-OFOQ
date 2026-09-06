using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryCheckoutLockRepository :
    ICheckoutLockRepository
{
    private readonly InMemoryCartStore
        _cartStore;

    private readonly InMemoryProductVariantStore
        _variantStore;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryCheckoutLockRepository(
        InMemoryCartStore cartStore,
        InMemoryProductVariantStore variantStore,
        ICurrentTenant currentTenant)
    {
        _cartStore =
            cartStore;

        _variantStore =
            variantStore;

        _currentTenant =
            currentTenant;
    }

    public Task<Cart?> GetActiveCartForUpdateAsync(
        UserId customerUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_cartStore.SyncRoot)
        {
            var cart =
                _cartStore.Items
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                            tenantId &&
                            item.CustomerUserId ==
                            customerUserId &&
                            item.Status ==
                            CartStatus.Active);

            return Task.FromResult(
                cart);
        }
    }

    public Task<IReadOnlyList<ProductVariant>>
        GetVariantsForUpdateAsync(
            IReadOnlyCollection<ProductVariantId> variantIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            variantIds);

        var tenantId =
            GetRequiredTenantId();

        lock (_variantStore.SyncRoot)
        {
            IReadOnlyList<ProductVariant> variants =
                variantIds
                    .Distinct()
                    .OrderBy(
                        id =>
                            id.Value)
                    .Select(
                        id =>
                            _variantStore.Items
                                .SingleOrDefault(
                                    item =>
                                        item.TenantId ==
                                        tenantId &&
                                        item.Id ==
                                        id &&
                                        !item.IsDeleted))
                    .Where(
                        item =>
                            item is not null)
                    .Cast<ProductVariant>()
                    .ToArray();

            return Task.FromResult(
                variants);
        }
    }

    private TenantId GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}
