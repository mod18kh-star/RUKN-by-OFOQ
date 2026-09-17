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

    private ContentPage() { }

    private ContentPage(ContentPageId id, TenantId tenantId, string title, string slug, string body, string? seoTitle, string? seoDescription, DateTimeOffset createdAtUtc, Guid? createdByUserId) : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        TenantId = tenantId;
        Title = NormalizeRequired(title, MaxTitleLength, "Page title");
        Slug = NormalizeSlug(slug);
        Body = NormalizeBody(body);
        SeoTitle = NormalizeOptional(seoTitle, MaxSeoTitleLength, "SEO title");
        SeoDescription = NormalizeOptional(seoDescription, MaxSeoDescriptionLength, "SEO description");
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static ContentPage Create(TenantId tenantId, string title, string slug, string body, string? seoTitle, string? seoDescription, DateTimeOffset createdAtUtc, Guid? createdByUserId = null)
        => new(ContentPageId.New(), tenantId, title, slug, body, seoTitle, seoDescription, createdAtUtc, createdByUserId);

    public void Update(string title, string slug, string body, string? seoTitle, string? seoDescription, DateTimeOffset at, Guid? by)
    {
        Title = NormalizeRequired(title, MaxTitleLength, "Page title");
        Slug = NormalizeSlug(slug);
        Body = NormalizeBody(body);
        SeoTitle = NormalizeOptional(seoTitle, MaxSeoTitleLength, "SEO title");
        SeoDescription = NormalizeOptional(seoDescription, MaxSeoDescriptionLength, "SEO description");
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

    private void MarkUpdated(DateTimeOffset at, Guid? by) { UpdatedAtUtc = at; UpdatedByUserId = by; }
    private static string NormalizeRequired(string value, int max, string label) { if(string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{label} is required."); var n=value.Trim().Normalize(NormalizationForm.FormKC); if(n.Length>max) throw new ArgumentException($"{label} cannot exceed {max} characters."); return n; }
    private static string NormalizeBody(string value) { var n=(value??string.Empty).Trim(); if(n.Length>MaxBodyLength) throw new ArgumentException($"Page body cannot exceed {MaxBodyLength} characters."); return n; }
    private static string? NormalizeOptional(string? value,int max,string label){ if(string.IsNullOrWhiteSpace(value))return null; var n=value.Trim().Normalize(NormalizationForm.FormKC); if(n.Length>max)throw new ArgumentException($"{label} cannot exceed {max} characters."); return n; }
    private static string NormalizeSlug(string value)
    {
        if(string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Page slug is required.",nameof(value));
        var n=value.Trim().ToLowerInvariant();
        if(n.Length>MaxSlugLength) throw new ArgumentException($"Page slug cannot exceed {MaxSlugLength} characters.",nameof(value));
        if(n.StartsWith('-')||n.EndsWith('-')||n.Any(c=>!(char.IsLetterOrDigit(c)||c=='-'))) throw new ArgumentException("Page slug may contain only letters, numbers and hyphens.",nameof(value));
        return n;
    }
}
