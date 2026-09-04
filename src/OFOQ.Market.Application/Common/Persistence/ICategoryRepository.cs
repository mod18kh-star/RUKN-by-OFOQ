using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default);

    Task<Category?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(
        string slug,
        CategoryId? excludingCategoryId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default);
}