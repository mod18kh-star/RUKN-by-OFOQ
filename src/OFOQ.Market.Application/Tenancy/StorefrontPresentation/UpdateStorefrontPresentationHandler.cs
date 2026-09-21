using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.StorefrontPresentation;

public sealed class UpdateStorefrontPresentationHandler
{
    private readonly ICurrentTenant
        _currentTenant;

    private readonly ITenantStorefrontPresentationRepository
        _repository;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public UpdateStorefrontPresentationHandler(
        ICurrentTenant currentTenant,
        ITenantStorefrontPresentationRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _currentTenant =
            currentTenant;

        _repository =
            repository;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<StorefrontPresentationResult> HandleAsync(
        UpdateStorefrontPresentationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (command.ActorUserId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(command));
        }

        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "Tenant context is required.");
        }

        var tenantId =
            _currentTenant.TenantId.Value;

        var presentation =
            await _repository
                .GetAsync(
                    cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        if (presentation is null)
        {
            presentation =
                TenantStorefrontPresentation.Create(
                    tenantId,
                    command.LogoUrl,
                    command.CoverImageUrl,
                    command.Announcement,
                    command.PrimaryColor,
                    command.AccentColor,
                    command.ThemePresetCode,
                    command.FontCode,
                    command.ShowCategoriesOnHome,
                    command.ShowProductsOnHome,
                    command.CategorySectionTitle,
                    command.ProductSectionTitle,
                    now,
                    command.ActorUserId);

            await _repository
                .AddAsync(
                    presentation,
                    cancellationToken);
        }
        else
        {
            if (presentation.TenantId !=
                tenantId)
            {
                throw new TenantScopeViolationException(
                    "Cross-tenant storefront presentation access was blocked.");
            }

            presentation.Update(
                command.LogoUrl,
                command.CoverImageUrl,
                command.Announcement,
                command.PrimaryColor,
                command.AccentColor,
                command.ThemePresetCode,
                command.FontCode,
                command.ShowCategoriesOnHome,
                command.ShowProductsOnHome,
                command.CategorySectionTitle,
                command.ProductSectionTitle,
                now,
                command.ActorUserId);
        }

        presentation.SetVisualContentJson(command.VisualContentJson);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return GetStorefrontPresentationHandler.Map(
            presentation);
    }
}
