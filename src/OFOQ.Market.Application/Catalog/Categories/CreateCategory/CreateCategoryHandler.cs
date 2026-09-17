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

        var categories =
            await _categoryRepository
                .GetAllAsync(
                    cancellationToken);

        if (command.ParentCategoryId.HasValue &&
            categories.All(
                category =>
                    category.Id !=
                    command.ParentCategoryId.Value))
        {
            throw new CategoryParentNotFoundException(
                command.ParentCategoryId.Value);
        }

        var now =
            _timeProvider.GetUtcNow();

        /*
         * Legacy SortOrder remains accepted temporarily so older
         * clients/tests continue to work during the V1 contract
         * migration. New clients use Position. When neither is
         * supplied the category is appended to its sibling group.
         */
        if (!command.Position.HasValue &&
            command.LegacySortOrder.HasValue)
        {
            var legacyCategory =
                Category.Create(
                    tenantId,
                    command.Name,
                    command.Slug,
                    now,
                    command.ParentCategoryId,
                    command.LegacySortOrder.Value,
                    command.ActorUserId.Value,
                    command.ImageUrl);

            await _categoryRepository
                .AddAsync(
                    legacyCategory,
                    cancellationToken);

            await _unitOfWork
                .SaveChangesAsync(
                    cancellationToken);

            return Map(
                legacyCategory);
        }

        var siblings =
            categories
                .Where(
                    category =>
                        category.ParentCategoryId ==
                        command.ParentCategoryId)
                .OrderBy(
                    category =>
                        category.SortOrder)
                .ThenBy(
                    category =>
                        category.CreatedAtUtc)
                .ThenBy(
                    category =>
                        category.Id.Value)
                .ToList();

        CategoryPosition.Normalize(
            siblings,
            now,
            command.ActorUserId.Value);

        var position =
            CategoryPosition.Resolve(
                command.Position,
                siblings.Count + 1);

        for (var index =
                 position - 1;
             index < siblings.Count;
             index++)
        {
            siblings[index]
                .ChangeSortOrder(
                    index + 2,
                    now,
                    command.ActorUserId.Value);
        }

        var category =
            Category.Create(
                tenantId,
                command.Name,
                command.Slug,
                now,
                command.ParentCategoryId,
                position,
                command.ActorUserId.Value,
                command.ImageUrl);

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
            category.CreatedAtUtc,
            category.ImageUrl);
    }
}
