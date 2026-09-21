namespace OFOQ.Market.Contracts.Tenancy;

public sealed record StorefrontPresentationResponse(
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
    string ProductSectionTitle,
    string VisualContentJson = "{}");
