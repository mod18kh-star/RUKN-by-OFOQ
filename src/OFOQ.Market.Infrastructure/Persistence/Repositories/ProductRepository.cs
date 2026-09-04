using System.Text;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository :
    IProductRepository
{
    private readonly MarketDbContext _dbContext;

    public ProductRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .SingleOrDefaultAsync(
                product =>
                    product.Id == productId,
                cancellationToken);
    }

    public Task<Product?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            slug);

        var normalized =
            NormalizeSlug(slug);

        return _dbContext.Products
            .SingleOrDefaultAsync(
                product =>
                    product.Slug == normalized,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Product>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .OrderByDescending(
                product =>
                    product.CreatedAtUtc)
            .ToListAsync(
                cancellationToken);
    }

    public Task<bool> SlugExistsAsync(
        string slug,
        ProductId? excludingProductId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            slug);

        var normalized =
            NormalizeSlug(slug);

        var query =
            _dbContext.Products
                .Where(
                    product =>
                        product.Slug == normalized);

        if (excludingProductId.HasValue)
        {
            var productId =
                excludingProductId.Value;

            query =
                query.Where(
                    product =>
                        product.Id != productId);
        }

        return query.AnyAsync(
            cancellationToken);
    }

    public Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            product);

        return _dbContext.Products
            .AddAsync(
                product,
                cancellationToken)
            .AsTask();
    }

    private static string NormalizeSlug(
        string slug)
    {
        return slug
            .Trim()
            .Normalize(
                NormalizationForm.FormKC)
            .ToLowerInvariant();
    }
}