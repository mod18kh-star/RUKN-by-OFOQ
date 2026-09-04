using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository :
    ICategoryRepository
{
    private readonly MarketDbContext
        _dbContext;

    public CategoryRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<Category?> GetByIdAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Categories
            .SingleOrDefaultAsync(
                category =>
                    category.Id ==
                    categoryId,
                cancellationToken);
    }

    public Task<Category?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            slug);

        var normalizedSlug =
            slug
                .Trim()
                .Normalize(
                    System.Text.NormalizationForm.FormKC)
                .ToLowerInvariant();

        return _dbContext
            .Categories
            .SingleOrDefaultAsync(
                category =>
                    category.Slug ==
                    normalizedSlug,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Category>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Categories
            .OrderBy(
                category =>
                    category.SortOrder)
            .ThenBy(
                category =>
                    category.Name)
            .ToListAsync(
                cancellationToken);
    }

    public Task<bool> SlugExistsAsync(
        string slug,
        CategoryId? excludingCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            slug);

        var normalizedSlug =
            slug
                .Trim()
                .Normalize(
                    System.Text.NormalizationForm.FormKC)
                .ToLowerInvariant();

        var query =
            _dbContext
                .Categories
                .Where(
                    category =>
                        category.Slug ==
                        normalizedSlug);

        if (excludingCategoryId.HasValue)
        {
            var categoryId =
                excludingCategoryId.Value;

            query =
                query.Where(
                    category =>
                        category.Id !=
                        categoryId);
        }

        return query.AnyAsync(
            cancellationToken);
    }

    public async Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            category);

        await _dbContext
            .Categories
            .AddAsync(
                category,
                cancellationToken);
    }
}