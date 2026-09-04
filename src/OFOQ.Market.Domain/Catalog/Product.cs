using System.Text;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class Product :
    Entity<ProductId>,
    ITenantDataScoped,
    IAuditable,
    ISoftDeletable
{
    private Product()
    {
    }

    private Product(
        ProductId id,
        TenantId tenantId,
        string name,
        string slug,
        string? description,
        CategoryId? categoryId,
        Money price,
        Money? compareAtPrice,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
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

        Description =
            NormalizeDescription(
                description);

        CategoryId =
            categoryId;

        ValidatePricing(
            price,
            compareAtPrice);

        Price =
            price;

        CompareAtPrice =
            compareAtPrice;

        Status =
            ProductStatus.Draft;

        IsVisible =
            false;

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

    public string? Description { get; private set; }

    public CategoryId? CategoryId { get; private set; }

    public Money Price { get; private set; }

    public Money? CompareAtPrice { get; private set; }

    public ProductStatus Status { get; private set; }

    public bool IsVisible { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static Product Create(
        TenantId tenantId,
        string name,
        string slug,
        Money price,
        DateTimeOffset createdAtUtc,
        CategoryId? categoryId = null,
        string? description = null,
        Money? compareAtPrice = null,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        return new Product(
            ProductId.New(),
            tenantId,
            name,
            slug,
            description,
            categoryId,
            price,
            compareAtPrice,
            createdAtUtc,
            createdByUserId);
    }

    public void Rename(
        string name,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var normalized =
            NormalizeName(
                name);

        if (Name ==
            normalized)
        {
            return;
        }

        Name =
            normalized;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeSlug(
        string slug,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var normalized =
            NormalizeSlug(
                slug);

        if (Slug ==
            normalized)
        {
            return;
        }

        Slug =
            normalized;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeDescription(
        string? description,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var normalized =
            NormalizeDescription(
                description);

        if (Description ==
            normalized)
        {
            return;
        }

        Description =
            normalized;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeCategory(
        CategoryId? categoryId,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (CategoryId ==
            categoryId)
        {
            return;
        }

        CategoryId =
            categoryId;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void SetPricing(
        Money price,
        Money? compareAtPrice,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        ValidatePricing(
            price,
            compareAtPrice);

        if (Price ==
                price &&
            CompareAtPrice ==
                compareAtPrice)
        {
            return;
        }

        Price =
            price;

        CompareAtPrice =
            compareAtPrice;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Publish(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        if (Status ==
                ProductStatus.Published &&
            IsVisible)
        {
            return;
        }

        Status =
            ProductStatus.Published;

        IsVisible =
            true;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void MoveToDraft(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        if (Status ==
                ProductStatus.Draft &&
            !IsVisible)
        {
            return;
        }

        Status =
            ProductStatus.Draft;

        IsVisible =
            false;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Archive(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        if (Status ==
                ProductStatus.Archived &&
            !IsVisible)
        {
            return;
        }

        Status =
            ProductStatus.Archived;

        IsVisible =
            false;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Show(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        if (Status !=
            ProductStatus.Published)
        {
            throw new InvalidOperationException(
                "Only a published product can be visible.");
        }

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
        EnsureNotDeleted();

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

        IsVisible =
            false;

        DeletedAtUtc =
            deletedAtUtc;

        DeletedByUserId =
            deletedByUserId;

        MarkUpdated(
            deletedAtUtc,
            deletedByUserId);
    }

    private static void ValidatePricing(
        Money price,
        Money? compareAtPrice)
    {
        if (price.Currency.IsEmpty)
        {
            throw new ArgumentException(
                "Product price currency is required.",
                nameof(price));
        }

        if (!compareAtPrice.HasValue)
        {
            return;
        }

        var comparePrice =
            compareAtPrice.Value;

        if (comparePrice.Currency !=
            price.Currency)
        {
            throw new ArgumentException(
                "Product price and compare-at price must use the same currency.",
                nameof(compareAtPrice));
        }

        if (comparePrice.Amount <=
            price.Amount)
        {
            throw new ArgumentException(
                "Compare-at price must be greater than the product price.",
                nameof(compareAtPrice));
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "A deleted product cannot be modified.");
        }
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
                "Product name is required.",
                nameof(name));
        }

        var normalized =
            name.Trim();

        if (normalized.Length > 200)
        {
            throw new ArgumentException(
                "Product name cannot exceed 200 characters.",
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
                "Product slug is required.",
                nameof(slug));
        }

        var normalized =
            slug
                .Trim()
                .Normalize(
                    NormalizationForm.FormKC)
                .ToLowerInvariant();

        if (normalized.Length > 160)
        {
            throw new ArgumentException(
                "Product slug cannot exceed 160 characters.",
                nameof(slug));
        }

        if (normalized.StartsWith('-') ||
            normalized.EndsWith('-') ||
            normalized.Contains(
                "--",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Product slug format is invalid.",
                nameof(slug));
        }

        foreach (var character in normalized)
        {
            if (!char.IsLetterOrDigit(
                    character) &&
                character != '-')
            {
                throw new ArgumentException(
                    "Product slug may contain only letters, numbers and hyphens.",
                    nameof(slug));
            }
        }

        return normalized;
    }

    private static string? NormalizeDescription(
        string? description)
    {
        if (string.IsNullOrWhiteSpace(
                description))
        {
            return null;
        }

        var normalized =
            description.Trim();

        if (normalized.Length > 5000)
        {
            throw new ArgumentException(
                "Product description cannot exceed 5000 characters.",
                nameof(description));
        }

        return normalized;
    }
}