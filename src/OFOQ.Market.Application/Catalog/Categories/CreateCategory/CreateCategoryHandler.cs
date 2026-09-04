using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Categories.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly ICategoryRepository
        _categoryRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CreateCategoryHandler(
        ICategoryRepository categoryRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _categoryRepository =
            categoryRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<CategoryResult> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to create a category.");
        }

        var tenantId =
            _currentTenant.TenantId.Value;

        var slugExists =
            await _categoryRepository
                .SlugExistsAsync(
                    command.Slug,
                    excludingCategoryId: null,
                    cancellationToken);

        if (slugExists)
        {
            throw new CategorySlugAlreadyExistsException(
                command.Slug);
        }

        if (command.ParentCategoryId.HasValue)
        {
            var parent =
                await _categoryRepository
                    .GetByIdAsync(
                        command.ParentCategoryId.Value,
                        cancellationToken);

            /*
             * The repository is tenant-filtered.
             *
             * Therefore a parent from another tenant is
             * indistinguishable from a nonexistent parent.
             */
            if (parent is null)
            {
                throw new CategoryParentNotFoundException(
                    command.ParentCategoryId.Value);
            }
        }

        var now =
            _timeProvider.GetUtcNow();

        var category =
            Category.Create(
                tenantId,
                command.Name,
                command.Slug,
                now,
                command.ParentCategoryId,
                command.SortOrder,
                command.ActorUserId.Value);

        await _categoryRepository
            .AddAsync(
                category,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Map(
            category);
    }

    private static CategoryResult Map(
        Category category)
    {
        return new CategoryResult(
            category.Id,
            category.Name,
            category.Slug,
            category.ParentCategoryId,
            category.SortOrder,
            category.IsVisible,
            category.CreatedAtUtc);
    }
}