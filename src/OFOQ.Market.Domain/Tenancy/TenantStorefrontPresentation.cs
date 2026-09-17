using System.Text.RegularExpressions;
using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Tenancy;

public sealed partial class TenantStorefrontPresentation :
    Entity<TenantStorefrontPresentationId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxUrlLength = 2048;
    public const int MaxAnnouncementLength = 240;
    public const int MaxSectionTitleLength = 120;
    public const int MaxCodeLength = 80;

    public const string DefaultThemePresetCode =
        "editorial";

    public const string DefaultFontCode =
        "plex";

    public const string DefaultCategorySectionTitle =
        "تصفح الأقسام";

    public const string DefaultProductSectionTitle =
        "منتجات المتجر";

    private TenantStorefrontPresentation()
    {
    }

    private TenantStorefrontPresentation(
        TenantStorefrontPresentationId id,
        TenantId tenantId,
        string? logoUrl,
        string? coverImageUrl,
        string? announcement,
        string? primaryColor,
        string? accentColor,
        string? themePresetCode,
        string? fontCode,
        bool showCategoriesOnHome,
        bool showProductsOnHome,
        string? categorySectionTitle,
        string? productSectionTitle,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        TenantId =
            tenantId;

        Apply(
            logoUrl,
            coverImageUrl,
            announcement,
            primaryColor,
            accentColor,
            themePresetCode,
            fontCode,
            showCategoriesOnHome,
            showProductsOnHome,
            categorySectionTitle,
            productSectionTitle);

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public string? LogoUrl { get; private set; }

    public string? CoverImageUrl { get; private set; }

    public string? Announcement { get; private set; }

    public string? PrimaryColor { get; private set; }

    public string? AccentColor { get; private set; }

    public string ThemePresetCode { get; private set; } =
        DefaultThemePresetCode;

    public string FontCode { get; private set; } =
        DefaultFontCode;

    public bool ShowCategoriesOnHome { get; private set; }

    public bool ShowProductsOnHome { get; private set; }

    public string CategorySectionTitle { get; private set; } =
        DefaultCategorySectionTitle;

    public string ProductSectionTitle { get; private set; } =
        DefaultProductSectionTitle;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantStorefrontPresentation Create(
        TenantId tenantId,
        string? logoUrl,
        string? coverImageUrl,
        string? announcement,
        string? primaryColor,
        string? accentColor,
        string? themePresetCode,
        string? fontCode,
        bool showCategoriesOnHome,
        bool showProductsOnHome,
        string? categorySectionTitle,
        string? productSectionTitle,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantStorefrontPresentation(
            TenantStorefrontPresentationId.New(),
            tenantId,
            logoUrl,
            coverImageUrl,
            announcement,
            primaryColor,
            accentColor,
            themePresetCode,
            fontCode,
            showCategoriesOnHome,
            showProductsOnHome,
            categorySectionTitle,
            productSectionTitle,
            createdAtUtc,
            createdByUserId);
    }

    public void Update(
        string? logoUrl,
        string? coverImageUrl,
        string? announcement,
        string? primaryColor,
        string? accentColor,
        string? themePresetCode,
        string? fontCode,
        bool showCategoriesOnHome,
        bool showProductsOnHome,
        string? categorySectionTitle,
        string? productSectionTitle,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        Apply(
            logoUrl,
            coverImageUrl,
            announcement,
            primaryColor,
            accentColor,
            themePresetCode,
            fontCode,
            showCategoriesOnHome,
            showProductsOnHome,
            categorySectionTitle,
            productSectionTitle);

        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private void Apply(
        string? logoUrl,
        string? coverImageUrl,
        string? announcement,
        string? primaryColor,
        string? accentColor,
        string? themePresetCode,
        string? fontCode,
        bool showCategoriesOnHome,
        bool showProductsOnHome,
        string? categorySectionTitle,
        string? productSectionTitle)
    {
        LogoUrl =
            NormalizeUrl(
                logoUrl,
                "Logo URL");

        CoverImageUrl =
            NormalizeUrl(
                coverImageUrl,
                "Cover image URL");

        Announcement =
            NormalizeOptional(
                announcement,
                MaxAnnouncementLength,
                "Announcement");

        PrimaryColor =
            NormalizeColor(
                primaryColor,
                "Primary color");

        AccentColor =
            NormalizeColor(
                accentColor,
                "Accent color");

        ThemePresetCode =
            NormalizeCode(
                themePresetCode,
                DefaultThemePresetCode,
                "Theme preset code");

        FontCode =
            NormalizeCode(
                fontCode,
                DefaultFontCode,
                "Font code");

        ShowCategoriesOnHome =
            showCategoriesOnHome;

        ShowProductsOnHome =
            showProductsOnHome;

        CategorySectionTitle =
            NormalizeRequired(
                categorySectionTitle,
                DefaultCategorySectionTitle,
                MaxSectionTitleLength,
                "Category section title");

        ProductSectionTitle =
            NormalizeRequired(
                productSectionTitle,
                DefaultProductSectionTitle,
                MaxSectionTitleLength,
                "Product section title");
    }

    private static string? NormalizeUrl(
        string? value,
        string fieldName)
    {
        var normalized =
            NormalizeOptional(
                value,
                MaxUrlLength,
                fieldName);

        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException(
                $"{fieldName} must be an absolute HTTP or HTTPS URL.");
        }

        return uri.AbsoluteUri;
    }

    private static string? NormalizeColor(
        string? value,
        string fieldName)
    {
        var normalized =
            NormalizeOptional(
                value,
                7,
                fieldName);

        if (normalized is null)
        {
            return null;
        }

        if (!HexColorRegex().IsMatch(
                normalized))
        {
            throw new ArgumentException(
                $"{fieldName} must use #RRGGBB format.");
        }

        return normalized.ToUpperInvariant();
    }

    private static string NormalizeCode(
        string? value,
        string defaultValue,
        string fieldName)
    {
        var normalized =
            string.IsNullOrWhiteSpace(
                value)
            ? defaultValue
            : value.Trim().ToLowerInvariant();

        if (normalized.Length >
            MaxCodeLength ||
            !CodeRegex().IsMatch(
                normalized))
        {
            throw new ArgumentException(
                $"{fieldName} is invalid.");
        }

        return normalized;
    }

    private static string NormalizeRequired(
        string? value,
        string defaultValue,
        int maxLength,
        string fieldName)
    {
        var normalized =
            string.IsNullOrWhiteSpace(
                value)
            ? defaultValue
            : value.Trim();

        if (normalized.Length >
            maxLength)
        {
            throw new ArgumentException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        var normalized =
            value.Trim();

        if (normalized.Length >
            maxLength)
        {
            throw new ArgumentException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    [GeneratedRegex(
        "^#[0-9A-Fa-f]{6}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();

    [GeneratedRegex(
        "^[a-z0-9][a-z0-9-]*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();
}
