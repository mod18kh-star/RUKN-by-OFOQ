using System.Text.RegularExpressions;
using System.Text.Json;
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
    public const int MaxVisualContentLength = 16000;

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

    public string VisualContentJson { get; private set; } = "{}";

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

    // Editorial content is data, never executable markup. Keep the allowlist on
    // the server; the browser's preview is not a security boundary.
    public void SetVisualContentJson(string? json)
    {
        if (json is null)
        {
            return; // Backward-compatible callers keep the current content.
        }

        if (json.Length > MaxVisualContentLength)
        {
            throw new ArgumentException("Visual content is too large.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Visual content must be an object.");
            }

            var limits = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["bodyTextColor"] = 7,
                ["primaryButtonColor"] = 7,
                ["primaryButtonTextColor"] = 7,
                ["accentButtonTextColor"] = 7,
                ["heroEyebrow"] = 100,
                ["heroTitle"] = 180,
                ["heroDescription"] = 500,
                ["heroCtaLabel"] = 80,
                ["heroCtaHref"] = 300,
                ["heroSecondaryImage"] = MaxUrlLength,
                ["categoryEyebrow"] = 80,
                ["productEyebrow"] = 80,
                ["footerDescription"] = 500,
                ["sectionOrder"] = 100,
                ["smartBand4Body"] = 200,
                ["smartBand4Title"] = 80,
                ["smartBand3Body"] = 200,
                ["smartBand3Title"] = 80,
                ["smartBand2Body"] = 200,
                ["smartBand2Title"] = 80,
                ["smartBand1Body"] = 200,
                ["smartBand1Title"] = 80,
                ["flagshipBand3Body"] = 200,
                ["flagshipBand3Title"] = 80,
                ["flagshipBand2Body"] = 200,
                ["flagshipBand2Title"] = 80,
                ["flagshipBand1Body"] = 200,
                ["flagshipBand1Title"] = 80,
                ["heroFeaturedCaption"] = 180,
                ["heroStat1"] = 50,
                ["heroStat2"] = 50,
                ["heroStat3"] = 50,
                ["heroNote1Title"] = 80,
                ["heroNote1Body"] = 180,
                ["heroNote2Title"] = 80,
                ["heroNote2Body"] = 180,
                ["heroNote3Title"] = 80,
                ["heroNote3Body"] = 180,
                ["heroShowcaseMode"] = 20,
                ["heroShowcaseText"] = 500,
                ["heroShowcaseImage"] = 2048,
                ["featuredProductIds"] = 180,
                ["productLayout"] = 32,
                ["productCardStyle"] = 32,
                ["categoryCardLayout"] = 32,
            };

            foreach (var field in document.RootElement.EnumerateObject())
            {
                if (!limits.TryGetValue(field.Name, out var maxLength) ||
                    field.Value.ValueKind != JsonValueKind.String)
                {
                    throw new ArgumentException("Unsupported visual content field.");
                }

                var value = field.Value.GetString() ?? "";
                if (value.Length > maxLength ||
                    value.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
                {
                    throw new ArgumentException("Visual content field is invalid.");
                }

                if (field.Name is "bodyTextColor" or "primaryButtonColor" or "primaryButtonTextColor" or "accentButtonTextColor")
                {
                    if (value.Length > 0 &&
                        (value.Length != 7 || value[0] != '#' ||
                         value.Skip(1).Any(character => !Uri.IsHexDigit(character))))
                    {
                        throw new ArgumentException("Invalid storefront color value.");
                    }
                }

                if (field.Name == "sectionOrder" &&
                    !new[] { "categories", "products", "banner", "story" }
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .SequenceEqual(value.Split(',').OrderBy(part => part, StringComparer.Ordinal)))
                {
                    throw new ArgumentException("Invalid storefront section order.");
                }

                if (field.Name == "heroCtaHref" && value.Length > 0 &&
                    (value.Contains('\\') ||
                     !(value.StartsWith('#') ||
                       (value.StartsWith('/') && !value.StartsWith("//")))))
                {
                    throw new ArgumentException("Hero button link must be an internal URL.");
                }

                if (field.Name == "heroShowcaseMode" &&
                    value is not ("default" or "image" or "text" or "product" or "products"))
                {
                    throw new ArgumentException("Invalid hero showcase mode.");
                }

                if (field.Name == "productLayout" &&
                    value is not ("theme-default" or "grid-3" or "featured-grid" or "horizontal" or "spotlight"))
                {
                    throw new ArgumentException("Invalid product layout.");
                }

                if (field.Name == "productCardStyle" &&
                    value is not ("theme-default" or "minimal" or "editorial" or "commerce" or "compact" or "technical" or "mobile-flagship" or "mobile-market" or "vertical-signature" or "vertical-market"))
                {
                    throw new ArgumentException("Invalid product card style.");
                }

                if (field.Name == "categoryCardLayout" &&
                    value is not ("theme-default" or "grid" or "compact"))
                {
                    throw new ArgumentException("Invalid category layout.");
                }

                if (field.Name == "featuredProductIds" && value.Length > 0 &&
                    (value.Split(',').Length > 4 ||
                     value.Split(',').Any(id => !Guid.TryParse(id, out _)) ||
                     value.Split(',').Distinct(StringComparer.OrdinalIgnoreCase).Count() != value.Split(',').Length))
                {
                    throw new ArgumentException("Invalid featured product identifiers.");
                }

                if (field.Name == "heroShowcaseImage" && value.Length > 0 &&
                    (!Uri.TryCreate(value, UriKind.Absolute, out var showcaseImageUri) ||
                     (showcaseImageUri.Scheme is not ("https" or "http")) ||
                     (showcaseImageUri.Scheme == "http" &&
                      showcaseImageUri.Host is not ("localhost" or "127.0.0.1"))))
                {
                    throw new ArgumentException("Hero showcase image must be HTTPS or a localhost development URL.");
                }

                if (field.Name == "heroSecondaryImage" && value.Length > 0 &&
                    (!Uri.TryCreate(value, UriKind.Absolute, out var imageUri) ||
                     imageUri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new ArgumentException("Hero image must be an HTTPS URL.");
                }
            }

            VisualContentJson = JsonSerializer.Serialize(document.RootElement);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Visual content must be valid JSON.", exception);
        }
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
