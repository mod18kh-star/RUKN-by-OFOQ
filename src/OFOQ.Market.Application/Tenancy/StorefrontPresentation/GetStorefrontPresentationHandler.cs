using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.StorefrontPresentation;

public sealed class GetStorefrontPresentationHandler
{
    private readonly ICurrentTenant
        _currentTenant;

    private readonly ITenantStorefrontPresentationRepository
        _repository;

    public GetStorefrontPresentationHandler(
        ICurrentTenant currentTenant,
        ITenantStorefrontPresentationRepository repository)
    {
        _currentTenant =
            currentTenant;

        _repository =
            repository;
    }

    public async Task<StorefrontPresentationResult> HandleAsync(
        CancellationToken cancellationToken = default)
    {
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

        if (presentation is null)
        {
            return new StorefrontPresentationResult(
                tenantId.Value,
                null,
                null,
                null,
                null,
                null,
                TenantStorefrontPresentation.DefaultThemePresetCode,
                TenantStorefrontPresentation.DefaultFontCode,
                true,
                true,
                TenantStorefrontPresentation.DefaultCategorySectionTitle,
                TenantStorefrontPresentation.DefaultProductSectionTitle);
        }

        if (presentation.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant storefront presentation access was blocked.");
        }

        return Map(
            presentation);
    }

    public static StorefrontPresentationResult Map(
        TenantStorefrontPresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(
            presentation);

        return new StorefrontPresentationResult(
            presentation.TenantId.Value,
            presentation.LogoUrl,
            presentation.CoverImageUrl,
            presentation.Announcement,
            presentation.PrimaryColor,
            presentation.AccentColor,
            presentation.ThemePresetCode,
            presentation.FontCode,
            presentation.ShowCategoriesOnHome,
            presentation.ShowProductsOnHome,
            presentation.CategorySectionTitle,
            presentation.ProductSectionTitle);
    }
}
