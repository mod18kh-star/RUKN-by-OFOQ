using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class CheckoutLockRepository :
    ICheckoutLockRepository
{
    private readonly MarketDbContext
        _dbContext;

    public CheckoutLockRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<Cart?>
        GetActiveCartForUpdateAsync(
            UserId customerUserId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        if (customerUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        /*
         * SELECT ... FOR UPDATE serializes checkout
         * attempts against this exact active Cart row.
         *
         * It must be called inside ITransactionExecutor.
         */
        var cart =
            await _dbContext
                .Carts
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM commerce_carts
                    WHERE
                        tenant_id = {tenantId.Value}
                        AND customer_user_id = {customerUserId.Value}
                        AND status = {(int)CartStatus.Active}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (cart is null)
        {
            return null;
        }

        await _dbContext
            .Entry(
                cart)
            .Collection(
                "_items")
            .LoadAsync(
                cancellationToken);

        return cart;
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
            return Array.Empty<
                ProductVariant>();
        }

        /*
         * Lock in deterministic UUID order.
         *
         * If two checkouts contain overlapping Variants,
         * acquiring locks in the same order greatly reduces
         * deadlock risk.
         */
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
                "A tenant context is required for checkout locking.");
        }

        return _dbContext
            .CurrentTenantId;
    }
}