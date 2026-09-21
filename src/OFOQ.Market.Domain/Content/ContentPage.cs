using System.Text;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Content;

public sealed class ContentPage : Entity<ContentPageId>, ITenantDataScoped, IAuditable
{
    public const int MaxTitleLength = 200;
    public const int MaxSlugLength = 160;
    public const int MaxBodyLength = 50000;
    public const int MaxSeoTitleLength = 200;
    public const int MaxSeoDescriptionLength = 500;
    public const int MaxHeroImageUrlLength = 2048;

    private ContentPage() { }

    private ContentPage(
        ContentPageId id,
        TenantId tenantId,
        string title,
        string slug,
        string body,
        string? seoTitle,
        string? seoDescription,
        ContentPageKind kind,
        string? heroImageUrl,
        bool showCustomerCount,
        bool showCompletedOrderCount,
        bool showUnitsSold,
        bool showAverageRating,
        bool showReviewCount,
        bool showCountryCount,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId) : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        TenantId = tenantId;
        ApplyContent(
            title,
            slug,
            body,
            seoTitle,
            seoDescription,
            kind,
            heroImageUrl,
            showCustomerCount,
            showCompletedOrderCount,
            showUnitsSold,
            showAverageRating,
            showReviewCount,
            showCountryCount);
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    public ContentPageKind Kind { get; private set; } = ContentPageKind.Standard;
    public string? HeroImageUrl { get; private set; }
    public bool ShowCustomerCount { get; private set; } = true;
    public bool ShowCompletedOrderCount { get; private set; } = true;
    public bool ShowUnitsSold { get; private set; } = true;
    public bool ShowAverageRating { get; private set; } = true;
    public bool ShowReviewCount { get; private set; } = true;
    public bool ShowCountryCount { get; private set; } = true;
    public bool IsPublished { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static ContentPage Create(
        TenantId tenantId,
        string title,
        string slug,
        string body,
        string? seoTitle,
        string? seoDescription,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
        => Create(
            tenantId,
            title,
            slug,
            body,
            seoTitle,
            seoDescription,
            ContentPageKind.Standard,
            null,
            true,
            true,
            true,
            true,
            true,
            true,
            createdAtUtc,
            createdByUserId);

    public static ContentPage Create(
        TenantId tenantId,
        string title,
        string slug,
        string body,
        string? seoTitle,
        string? seoDescription,
        ContentPageKind kind,
        string? heroImageUrl,
        bool showCustomerCount,
        bool showCompletedOrderCount,
        bool showUnitsSold,
        bool showAverageRating,
        bool showReviewCount,
        bool showCountryCount,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
        => new(
            ContentPageId.New(),
            tenantId,
            title,
            slug,
            body,
            seoTitle,
            seoDescription,
            kind,
            heroImageUrl,
            showCustomerCount,
            showCompletedOrderCount,
            showUnitsSold,
            showAverageRating,
            showReviewCount,
            showCountryCount,
            createdAtUtc,
            createdByUserId);

    public void Update(
        string title,
        string slug,
        string body,
        string? seoTitle,
        string? seoDescription,
        DateTimeOffset at,
        Guid? by)
        => Update(
            title,
            slug,
            body,
            seoTitle,
            seoDescription,
            Kind,
            HeroImageUrl,
            ShowCustomerCount,
            ShowCompletedOrderCount,
            ShowUnitsSold,
            ShowAverageRating,
            ShowReviewCount,
            ShowCountryCount,
            at,
            by);

    public void Update(
        string title,
        string slug,
        string body,
        string? seoTitle,
        string? seoDescription,
        ContentPageKind kind,
        string? heroImageUrl,
        bool showCustomerCount,
        bool showCompletedOrderCount,
        bool showUnitsSold,
        bool showAverageRating,
        bool showReviewCount,
        bool showCountryCount,
        DateTimeOffset at,
        Guid? by)
    {
        ApplyContent(
            title,
            slug,
            body,
            seoTitle,
            seoDescription,
            kind,
            heroImageUrl,
            showCustomerCount,
            showCompletedOrderCount,
            showUnitsSold,
            showAverageRating,
            showReviewCount,
            showCountryCount);
        MarkUpdated(at, by);
    }

    public void Publish(DateTimeOffset at, Guid? by)
    {
        if (IsPublished) return;
        IsPublished = true;
        PublishedAtUtc = at;
        MarkUpdated(at, by);
    }

    public void Unpublish(DateTimeOffset at, Guid? by)
    {
        if (!IsPublished) return;
        IsPublished = false;
        PublishedAtUtc = null;
        MarkUpdated(at, by);
    }

    private void ApplyContent(
        string title,
        string slug,
        string body,
        string? seoTitle,
        string? seoDescription,
        ContentPageKind kind,
        string? heroImageUrl,
        bool showCustomerCount,
        bool showCompletedOrderCount,
        bool showUnitsSold,
        bool showAverageRating,
        bool showReviewCount,
        bool showCountryCount)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Title = NormalizeRequired(title, MaxTitleLength, "Page title");
        Slug = NormalizeSlug(slug);
        Body = NormalizeBody(body);
        SeoTitle = NormalizeOptional(seoTitle, MaxSeoTitleLength, "SEO title");
        SeoDescription = NormalizeOptional(seoDescription, MaxSeoDescriptionLength, "SEO description");
        Kind = kind;
        HeroImageUrl = NormalizeImageUrl(heroImageUrl);
        ShowCustomerCount = showCustomerCount;
        ShowCompletedOrderCount = showCompletedOrderCount;
        ShowUnitsSold = showUnitsSold;
        ShowAverageRating = showAverageRating;
        ShowReviewCount = showReviewCount;
        ShowCountryCount = showCountryCount;
    }

    private void MarkUpdated(DateTimeOffset at, Guid? by)
    {
        UpdatedAtUtc = at;
        UpdatedByUserId = by;
    }

    private static string NormalizeRequired(string value, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} is required.");
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
        if (normalized.Length > max)
        {
            throw new ArgumentException($"{label} cannot exceed {max} characters.");
        }

        return normalized;
    }

    private static string NormalizeBody(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length > MaxBodyLength)
        {
            throw new ArgumentException($"Page body cannot exceed {MaxBodyLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
        if (normalized.Length > max)
        {
            throw new ArgumentException($"{label} cannot exceed {max} characters.");
        }

        return normalized;
    }

    private static string? NormalizeImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim();
        if (normalized.Length > MaxHeroImageUrlLength)
        {
            throw new ArgumentException($"Hero image URL cannot exceed {MaxHeroImageUrlLength} characters.");
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            return normalized;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Hero image URL must be an HTTP/HTTPS URL or an application-relative path.");
        }

        return normalized;
    }

    private static string NormalizeSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Page slug is required.", nameof(value));
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxSlugLength)
        {
            throw new ArgumentException($"Page slug cannot exceed {MaxSlugLength} characters.", nameof(value));
        }

        if (normalized.StartsWith('-') ||
            normalized.EndsWith('-') ||
            normalized.Any(c => !(char.IsLetterOrDigit(c) || c == '-')))
        {
            throw new ArgumentException("Page slug may contain only letters, numbers and hyphens.", nameof(value));
        }

        return normalized;
    }
}
