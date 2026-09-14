using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductImageRepository :
    IProductImageRepository
{
    private readonly MarketDbContext
        _dbContext;

    public ProductImageRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<ProductImage>>
        GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .ProductImages
            .Where(
                image =>
                    image.ProductId ==
                    productId)
            .OrderBy(
                image =>
                    image.SortOrder)
            .ThenBy(
                image =>
                    image.Id)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<ProductImage> images,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            images);

        await _dbContext
            .ProductImages
            .AddRangeAsync(
                images,
                cancellationToken);
    }

    public void RemoveRange(
        IReadOnlyCollection<ProductImage> images)
    {
        ArgumentNullException.ThrowIfNull(
            images);

        _dbContext
            .ProductImages
            .RemoveRange(
                images);
    }
}