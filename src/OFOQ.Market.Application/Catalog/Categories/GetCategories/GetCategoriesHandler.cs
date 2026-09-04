using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Catalog.Categories.GetCategories;

public sealed class GetCategoriesHandler
{
    private readonly ICategoryRepository
        _categoryRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetCategoriesHandler(
        ICategoryRepository categoryRepository,
        ICurrentTenant currentTenant)
    {
        _categoryRepository =
            categoryRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<IReadOnlyList<CategoryResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureTenantContext();

        var categories =
            await _categoryRepository
                .GetAllAsync(
                    cancellationToken);

        return categories
            .Select(
                category =>
                    new CategoryResult(
                        category.Id,
                        category.Name,
                        category.Slug,
                        category.ParentCategoryId,
                        category.SortOrder,
                        category.IsVisible,
                        category.CreatedAtUtc))
            .ToArray();
    }

    private void EnsureTenantContext()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to read categories.");
        }
    }
}