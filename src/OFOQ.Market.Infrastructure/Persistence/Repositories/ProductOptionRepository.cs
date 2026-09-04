using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductOptionRepository :
    IProductOptionRepository
{
    private readonly MarketDbContext _dbContext;

    public ProductOptionRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<ProductOption?> GetByIdAsync(
        ProductOptionId optionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductOptions
            .SingleOrDefaultAsync(
                option =>
                    option.Id == optionId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProductOption>>
        GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductOptions
            .Where(
                option =>
                    option.ProductId == productId)
            .OrderBy(
                option =>
                    option.SortOrder)
            .ThenBy(
                option =>
                    option.Name)
            .ToListAsync(
                cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        ProductId productId,
        string normalizedName,
        ProductOptionId? excludingOptionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedName);

        var query =
            _dbContext.ProductOptions
                .Where(
                    option =>
                        option.ProductId == productId &&
                        option.NormalizedName ==
                        normalizedName);

        if (excludingOptionId.HasValue)
        {
            var id =
                excludingOptionId.Value;

            query =
                query.Where(
                    option =>
                        option.Id != id);
        }

        return query.AnyAsync(
            cancellationToken);
    }

    public Task AddAsync(
        ProductOption option,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            option);

        return _dbContext.ProductOptions
            .AddAsync(
                option,
                cancellationToken)
            .AsTask();
    }
}