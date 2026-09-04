using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductOptionRepository
{
    Task<ProductOption?> GetByIdAsync(
        ProductOptionId optionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductOption>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        ProductId productId,
        string normalizedName,
        ProductOptionId? excludingOptionId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProductOption option,
        CancellationToken cancellationToken = default);
}