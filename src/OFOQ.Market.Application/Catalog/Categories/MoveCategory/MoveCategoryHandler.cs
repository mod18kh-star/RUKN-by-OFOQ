using OFOQ.Market.Application.Catalog.Categories.CreateCategory;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Categories.MoveCategory;

public sealed class MoveCategoryHandler
{
    private readonly ICategoryRepository
        _categoryRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public MoveCategoryHandler(
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
        MoveCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenantContext();

        var categories =
            await _categoryRepository
                .GetAllAsync(
                    cancellationToken);

        var category =
            categories
                .SingleOrDefault(
                    item =>
                        item.Id ==
                        command.CategoryId)
            ?? throw new CategoryNotFoundException(
                command.CategoryId);

        Category? newParent =
            null;

        if (command.ParentCategoryId.HasValue)
        {
            newParent =
                categories
                    .SingleOrDefault(
                        item =>
                            item.Id ==
                            command.ParentCategoryId.Value);

            if (newParent is null)
            {
                throw new CategoryParentNotFoundException(
                    command.ParentCategoryId.Value);
            }

            EnsureDoesNotCreateCycle(
                category,
                newParent,
                categories);
        }

        var now =
            _timeProvider.GetUtcNow();

        var oldParentCategoryId =
            category.ParentCategoryId;

        var oldSiblings =
            categories
                .Where(
                    item =>
                        item.Id != category.Id &&
                        item.ParentCategoryId ==
                        oldParentCategoryId)
                .OrderBy(
                    item =>
                        item.SortOrder)
                .ThenBy(
                    item =>
                        item.CreatedAtUtc)
                .ThenBy(
                    item =>
                        item.Id.Value)
                .ToList();

        CategoryPosition.Normalize(
            oldSiblings,
            now,
            command.ActorUserId.Value);

        if (oldParentCategoryId !=
            command.ParentCategoryId)
        {
            category.ChangeParent(
                command.ParentCategoryId,
                now,
                command.ActorUserId.Value);

            var newSiblings =
                categories
                    .Where(
                        item =>
                            item.Id != category.Id &&
                            item.ParentCategoryId ==
                            command.ParentCategoryId)
                    .OrderBy(
                        item =>
                            item.SortOrder)
                    .ThenBy(
                        item =>
                            item.CreatedAtUtc)
                    .ThenBy(
                        item =>
                            item.Id.Value)
                    .ToList();

            CategoryPosition.Normalize(
                newSiblings,
                now,
                command.ActorUserId.Value);

            var newPosition =
                CategoryPosition.Resolve(
                    command.Position,
                    newSiblings.Count + 1);

            newSiblings.Insert(
                newPosition - 1,
                category);

            CategoryPosition.Normalize(
                newSiblings,
                now,
                command.ActorUserId.Value);
        }
        else
        {
            var siblings =
                oldSiblings;

            var newPosition =
                CategoryPosition.Resolve(
                    command.Position,
                    siblings.Count + 1);

            siblings.Insert(
                newPosition - 1,
                category);

            CategoryPosition.Normalize(
                siblings,
                now,
                command.ActorUserId.Value);
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

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

    private static void EnsureDoesNotCreateCycle(
        Category category,
        Category newParent,
        IReadOnlyList<Category> categories)
    {
        if (newParent.Id ==
            category.Id)
        {
            throw new CategoryHierarchyCycleException();
        }

        var byId =
            categories.ToDictionary(
                item =>
                    item.Id);

        var current =
            newParent;

        var visited =
            new HashSet<CategoryId>();

        while (true)
        {
            if (!visited.Add(
                    current.Id))
            {
                throw new CategoryHierarchyCycleException();
            }

            if (current.Id ==
                category.Id)
            {
                throw new CategoryHierarchyCycleException();
            }

            if (!current.ParentCategoryId.HasValue)
            {
                return;
            }

            if (!byId.TryGetValue(
                    current.ParentCategoryId.Value,
                    out var parent))
            {
                return;
            }

            current =
                parent;
        }
    }

    private void EnsureTenantContext()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to move a category.");
        }
    }
}
