using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductOptionValueRepository
{
    Task<ProductOptionValue?> GetByIdAsync(
        ProductOptionValueId valueId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductOptionValue>> GetByOptionIdAsync(
        ProductOptionId optionId,
        CancellationToken cancellationToken = default);

    Task<bool> ValueExistsAsync(
        ProductOptionId optionId,
        string normalizedValue,
        ProductOptionValueId? excludingValueId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProductOptionValue value,
        CancellationToken cancellationToken = default);
}