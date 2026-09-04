using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductVariantOptionValueRepository :
    IProductVariantOptionValueRepository
{
    private readonly MarketDbContext _dbContext;

    public ProductVariantOptionValueRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<ProductVariantOptionValue>>
        GetByVariantIdAsync(
            ProductVariantId variantId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .ProductVariantOptionValues
            .Where(
                assignment =>
                    assignment.ProductVariantId ==
                    variantId)
            .ToListAsync(
                cancellationToken);
    }

    public Task<bool> ExistsForOptionAsync(
        ProductVariantId variantId,
        ProductOptionId optionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .ProductVariantOptionValues
            .AnyAsync(
                assignment =>
                    assignment.ProductVariantId ==
                        variantId &&
                    assignment.ProductOptionId ==
                        optionId,
                cancellationToken);
    }

    public Task AddAsync(
        ProductVariantOptionValue assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            assignment);

        return _dbContext
            .ProductVariantOptionValues
            .AddAsync(
                assignment,
                cancellationToken)
            .AsTask();
    }
}