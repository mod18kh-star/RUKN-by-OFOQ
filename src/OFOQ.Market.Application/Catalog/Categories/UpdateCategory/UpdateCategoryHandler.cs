using OFOQ.Market.Application.Catalog.Categories.CreateCategory;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Catalog.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler
{
    private readonly ICategoryRepository
        _categoryRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public UpdateCategoryHandler(
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
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenantContext();

        var category =
            await _categoryRepository
                .GetByIdAsync(
                    command.CategoryId,
                    cancellationToken)
            ?? throw new CategoryNotFoundException(
                command.CategoryId);

        var slugExists =
            await _categoryRepository
                .SlugExistsAsync(
                    command.Slug,
                    command.CategoryId,
                    cancellationToken);

        if (slugExists)
        {
            throw new CategorySlugAlreadyExistsException(
                command.Slug);
        }

        var now =
            _timeProvider.GetUtcNow();

        category.Rename(
            command.Name,
            now,
            command.ActorUserId.Value);

        category.ChangeSlug(
            command.Slug,
            now,
            command.ActorUserId.Value);

        category.ChangeImage(
            command.ImageUrl,
            now,
            command.ActorUserId.Value);

        if (command.IsVisible)
        {
            category.Show(
                now,
                command.ActorUserId.Value);
        }
        else
        {
            category.Hide(
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

    private void EnsureTenantContext()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to update a category.");
        }
    }
}
