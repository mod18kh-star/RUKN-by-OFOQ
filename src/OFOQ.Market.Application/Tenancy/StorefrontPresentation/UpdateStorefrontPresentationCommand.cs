namespace OFOQ.Market.Application.Tenancy.StorefrontPresentation;

public sealed record UpdateStorefrontPresentationCommand(
    string? LogoUrl,
    string? CoverImageUrl,
    string? Announcement,
    string? PrimaryColor,
    string? AccentColor,
    string? ThemePresetCode,
    string? FontCode,
    bool ShowCategoriesOnHome,
    bool ShowProductsOnHome,
    string? CategorySectionTitle,
    string? ProductSectionTitle,
    Guid ActorUserId,
    string? VisualContentJson = null);
