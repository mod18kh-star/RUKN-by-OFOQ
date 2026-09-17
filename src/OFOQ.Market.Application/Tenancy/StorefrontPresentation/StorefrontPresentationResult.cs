namespace OFOQ.Market.Application.Tenancy.StorefrontPresentation;

public sealed record StorefrontPresentationResult(
    Guid TenantId,
    string? LogoUrl,
    string? CoverImageUrl,
    string? Announcement,
    string? PrimaryColor,
    string? AccentColor,
    string ThemePresetCode,
    string FontCode,
    bool ShowCategoriesOnHome,
    bool ShowProductsOnHome,
    string CategorySectionTitle,
    string ProductSectionTitle);
