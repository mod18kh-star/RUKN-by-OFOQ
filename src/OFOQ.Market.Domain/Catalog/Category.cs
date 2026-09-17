using System.Text;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class Category :
    Entity<CategoryId>,
    ITenantDataScoped,
    IAuditable,
    ISoftDeletable
{
    private Category()
    {
    }

    private Category(
        CategoryId id,
        TenantId tenantId,
        string name,
        string slug,
        CategoryId? parentCategoryId,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId,
        string? imageUrl)
        : base(id)
    {
        TenantId =
            tenantId;

        Name =
            NormalizeName(
                name);

        Slug =
            NormalizeSlug(
                slug);

        ParentCategoryId =
            parentCategoryId;

        SortOrder =
            NormalizeSortOrder(
                sortOrder);

        ImageUrl =
            NormalizeImageUrl(
                imageUrl);

        IsVisible =
            true;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public string Name { get; private set; } =
        string.Empty;

    public string Slug { get; private set; } =
        string.Empty;

    public CategoryId? ParentCategoryId { get; private set; }

    public int SortOrder { get; private set; }

    public string? ImageUrl { get; private set; }

    public bool IsVisible { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static Category Create(
        TenantId tenantId,
        string name,
        string slug,
        DateTimeOffset createdAtUtc,
        CategoryId? parentCategoryId = null,
        int sortOrder = 0,
        Guid? createdByUserId = null,
        string? imageUrl = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        return new Category(
            CategoryId.New(),
            tenantId,
            name,
            slug,
            parentCategoryId,
            sortOrder,
            createdAtUtc,
            createdByUserId,
            imageUrl);
    }

    public void Rename(
        string name,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        Name =
            NormalizeName(
                name);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeSlug(
        string slug,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var normalizedSlug =
            NormalizeSlug(
                slug);

        if (string.Equals(
                Slug,
                normalizedSlug,
                StringComparison.Ordinal))
        {
            return;
        }

        Slug =
            normalizedSlug;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeParent(
        CategoryId? parentCategoryId,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (parentCategoryId.HasValue &&
            parentCategoryId.Value == Id)
        {
            throw new ArgumentException(
                "A category cannot be its own parent.",
                nameof(parentCategoryId));
        }

        if (ParentCategoryId ==
            parentCategoryId)
        {
            return;
        }

        ParentCategoryId =
            parentCategoryId;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeSortOrder(
        int sortOrder,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var normalizedSortOrder =
            NormalizeSortOrder(
                sortOrder);

        if (SortOrder ==
            normalizedSortOrder)
        {
            return;
        }

        SortOrder =
            normalizedSortOrder;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }


    public void ChangeImage(
        string? imageUrl,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var normalized =
            NormalizeImageUrl(
                imageUrl);

        if (string.Equals(
                ImageUrl,
                normalized,
                StringComparison.Ordinal))
        {
            return;
        }

        ImageUrl =
            normalized;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Show(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (IsVisible)
        {
            return;
        }

        IsVisible =
            true;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Hide(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (!IsVisible)
        {
            return;
        }

        IsVisible =
            false;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Delete(
        DateTimeOffset deletedAtUtc,
        Guid? deletedByUserId = null)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted =
            true;

        DeletedAtUtc =
            deletedAtUtc;

        DeletedByUserId =
            deletedByUserId;

        MarkUpdated(
            deletedAtUtc,
            deletedByUserId);
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private static string NormalizeName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));
        }

        var normalized =
            name.Trim();

        if (normalized.Length > 160)
        {
            throw new ArgumentException(
                "Category name cannot exceed 160 characters.",
                nameof(name));
        }

        return normalized;
    }

    private static string NormalizeSlug(
        string slug)
    {
        if (string.IsNullOrWhiteSpace(
                slug))
        {
            throw new ArgumentException(
                "Category slug is required.",
                nameof(slug));
        }

        var normalized =
            slug
                .Trim()
                .Normalize(
                    NormalizationForm.FormKC)
                .ToLowerInvariant();

        if (normalized.Length > 120)
        {
            throw new ArgumentException(
                "Category slug cannot exceed 120 characters.",
                nameof(slug));
        }

        if (normalized.StartsWith('-') ||
            normalized.EndsWith('-') ||
            normalized.Contains(
                "--",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Category slug format is invalid.",
                nameof(slug));
        }

        foreach (var character in normalized)
        {
            if (!char.IsLetterOrDigit(
                    character) &&
                character != '-')
            {
                throw new ArgumentException(
                    "Category slug may contain only letters, numbers and hyphens.",
                    nameof(slug));
            }
        }

        return normalized;
    }

    private static string? NormalizeImageUrl(
        string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(
                imageUrl))
        {
            return null;
        }

        var normalized =
            imageUrl.Trim();

        if (normalized.Length >
            2048)
        {
            throw new ArgumentException(
                "Category image URL cannot exceed 2048 characters.",
                nameof(imageUrl));
        }

        if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "Category image URL must be an absolute HTTP or HTTPS URL.",
                nameof(imageUrl));
        }

        return normalized;
    }

    private static int NormalizeSortOrder(
        int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Category sort order cannot be negative.");
        }

        return sortOrder;
    }
}