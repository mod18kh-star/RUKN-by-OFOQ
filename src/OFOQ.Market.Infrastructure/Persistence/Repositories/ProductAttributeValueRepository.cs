using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductAttributeValueRepository :
    IProductAttributeValueRepository
{
    private readonly MarketDbContext
        _dbContext;

    public ProductAttributeValueRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<ProductAttributeValue>>
        GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        if (productId.IsEmpty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(productId));
        }

        return await _dbContext
            .Set<ProductAttributeValue>()
            .Where(
                value =>
                    value.ProductId ==
                    productId)
            .OrderBy(
                value =>
                    value.Key)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        ProductAttributeValue value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        await _dbContext
            .Set<ProductAttributeValue>()
            .AddAsync(
                value,
                cancellationToken);
    }

    public void Remove(
        ProductAttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        _dbContext
            .Set<ProductAttributeValue>()
            .Remove(
                value);
    }
}