using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Categories.GetCategoryById;

public sealed class GetCategoryByIdHandler
{
    private readonly ICategoryRepository
        _categoryRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetCategoryByIdHandler(
        ICategoryRepository categoryRepository,
        ICurrentTenant currentTenant)
    {
        _categoryRepository =
            categoryRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<CategoryResult?> HandleAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to read a category.");
        }

        var category =
            await _categoryRepository
                .GetByIdAsync(
                    categoryId,
                    cancellationToken);

        if (category is null)
        {
            return null;
        }

        return new CategoryResult(
            category.Id,
            category.Name,
            category.Slug,
            category.ParentCategoryId,
            category.SortOrder,
            category.IsVisible,
            category.CreatedAtUtc,
            category.ImageUrl);
    }
}