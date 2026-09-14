using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class OrderStateLockRepository :
    IOrderStateLockRepository
{
    private readonly MarketDbContext
        _dbContext;

    public OrderStateLockRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        if (orderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(orderId));
        }

        var order =
            await _dbContext
                .Orders
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM commerce_orders
                    WHERE
                        tenant_id = {tenantId.Value}
                        AND id = {orderId.Value}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (order is null)
        {
            return null;
        }

        await _dbContext
            .Entry(order)
            .Collection("_items")
            .LoadAsync(cancellationToken);

        await _dbContext
            .Entry(order)
            .Collection("_timeline")
            .LoadAsync(cancellationToken);

        await _dbContext
            .Entry(order)
            .Collection("_inventoryMovements")
            .LoadAsync(cancellationToken);

        return order;
    }

    public async Task<IReadOnlyList<ProductVariant>>
        GetVariantsForUpdateAsync(
            IReadOnlyCollection<ProductVariantId> variantIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            variantIds);

        var tenantId =
            GetRequiredTenantId();

        if (variantIds.Count == 0)
        {
            return Array.Empty<ProductVariant>();
        }

        var orderedIds =
            variantIds
                .Distinct()
                .OrderBy(
                    id =>
                        id.Value)
                .ToArray();

        var variants =
            new List<ProductVariant>(
                orderedIds.Length);

        foreach (var variantId in
                 orderedIds)
        {
            if (variantId.IsEmpty)
            {
                throw new ArgumentException(
                    "Product variant IDs cannot contain an empty ID.",
                    nameof(variantIds));
            }

            var variant =
                await _dbContext
                    .ProductVariants
                    .FromSqlInterpolated(
                        $"""
                        SELECT *
                        FROM catalog_product_variants
                        WHERE
                            tenant_id = {tenantId.Value}
                            AND id = {variantId.Value}
                        FOR UPDATE
                        """)
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (variant is not null)
            {
                variants.Add(
                    variant);
            }
        }

        return variants;
    }

    private OFOQ.Market.Domain.Tenancy.TenantId
        GetRequiredTenantId()
    {
        if (!_dbContext.HasCurrentTenant ||
            _dbContext.CurrentTenantId.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required for order state locking.");
        }

        return _dbContext.CurrentTenantId;
    }
}