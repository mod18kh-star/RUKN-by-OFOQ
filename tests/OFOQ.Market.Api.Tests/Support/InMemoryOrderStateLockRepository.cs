using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryOrderStateLockRepository :
    IOrderStateLockRepository
{
    private readonly InMemoryOrderStore
        _orderStore;

    private readonly InMemoryProductVariantStore
        _variantStore;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryOrderStateLockRepository(
        InMemoryOrderStore orderStore,
        InMemoryProductVariantStore variantStore,
        ICurrentTenant currentTenant)
    {
        _orderStore =
            orderStore;

        _variantStore =
            variantStore;

        _currentTenant =
            currentTenant;
    }

    public Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_orderStore.SyncRoot)
        {
            var order =
                _orderStore.Items
                    .Select(
                        item =>
                            item.Order)
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                            tenantId &&
                            item.Id ==
                            orderId);

            return Task.FromResult(
                order);
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
                                    variant =>
                                        variant.TenantId ==
                                        tenantId &&
                                        variant.Id ==
                                        id))
                    .Where(
                        variant =>
                            variant is not null)
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