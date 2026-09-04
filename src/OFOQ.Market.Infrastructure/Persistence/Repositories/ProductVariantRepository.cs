using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductVariantRepository :
    IProductVariantRepository
{
    private readonly MarketDbContext
        _dbContext;

    public ProductVariantRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<ProductVariant?> GetByIdAsync(
        ProductVariantId variantId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .ProductVariants
            .SingleOrDefaultAsync(
                variant =>
                    variant.Id ==
                    variantId,
                cancellationToken);
    }

    public Task<ProductVariant?> GetBySkuAsync(
        ProductSku sku,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .ProductVariants
            .SingleOrDefaultAsync(
                variant =>
                    EF.Property<string>(
                        variant,
                        "_skuValue") ==
                    sku.Value,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProductVariant>>
        GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .ProductVariants
            .Where(
                variant =>
                    variant.ProductId ==
                    productId)
            .OrderByDescending(
                variant =>
                    variant.IsDefault)
            .ThenBy(
                variant =>
                    variant.Name)
            .ToListAsync(
                cancellationToken);
    }

    public Task<bool> SkuExistsAsync(
        ProductSku sku,
        ProductVariantId? excludingVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext
                .ProductVariants
                .Where(
                    variant =>
                        EF.Property<string>(
                            variant,
                            "_skuValue") ==
                        sku.Value);

        if (excludingVariantId.HasValue)
        {
            var variantId =
                excludingVariantId.Value;

            query =
                query.Where(
                    variant =>
                        variant.Id !=
                        variantId);
        }

        return query.AnyAsync(
            cancellationToken);
    }

    public Task AddAsync(
        ProductVariant variant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            variant);

        return _dbContext
            .ProductVariants
            .AddAsync(
                variant,
                cancellationToken)
            .AsTask();
    }
}