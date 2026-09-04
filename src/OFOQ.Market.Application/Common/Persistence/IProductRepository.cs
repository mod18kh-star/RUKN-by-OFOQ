using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(
        string slug,
        ProductId? excludingProductId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);
}