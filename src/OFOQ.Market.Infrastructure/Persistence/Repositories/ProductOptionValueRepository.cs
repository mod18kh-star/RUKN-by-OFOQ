using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductOptionValueRepository :
    IProductOptionValueRepository
{
    private readonly MarketDbContext _dbContext;

    public ProductOptionValueRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<ProductOptionValue?> GetByIdAsync(
        ProductOptionValueId valueId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductOptionValues
            .SingleOrDefaultAsync(
                value =>
                    value.Id == valueId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProductOptionValue>>
        GetByOptionIdAsync(
            ProductOptionId optionId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductOptionValues
            .Where(
                value =>
                    value.ProductOptionId ==
                    optionId)
            .OrderBy(
                value =>
                    value.SortOrder)
            .ThenBy(
                value =>
                    value.Value)
            .ToListAsync(
                cancellationToken);
    }

    public Task<bool> ValueExistsAsync(
        ProductOptionId optionId,
        string normalizedValue,
        ProductOptionValueId? excludingValueId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedValue);

        var query =
            _dbContext.ProductOptionValues
                .Where(
                    value =>
                        value.ProductOptionId ==
                            optionId &&
                        value.NormalizedValue ==
                            normalizedValue);

        if (excludingValueId.HasValue)
        {
            var id =
                excludingValueId.Value;

            query =
                query.Where(
                    value =>
                        value.Id != id);
        }

        return query.AnyAsync(
            cancellationToken);
    }

    public Task AddAsync(
        ProductOptionValue value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        return _dbContext.ProductOptionValues
            .AddAsync(
                value,
                cancellationToken)
            .AsTask();
    }
}