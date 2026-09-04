using System.Text;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class ProductOption :
    Entity<ProductOptionId>,
    ITenantDataScoped,
    IAuditable,
    ISoftDeletable
{
    private ProductOption()
    {
    }

    private ProductOption(
        ProductOptionId id,
        TenantId tenantId,
        ProductId productId,
        string name,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        ProductId =
            productId;

        ApplyName(
            name);

        SortOrder =
            ValidateSortOrder(
                sortOrder);

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public ProductId ProductId { get; private set; }

    public string Name { get; private set; } =
        string.Empty;

    public string NormalizedName { get; private set; } =
        string.Empty;

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static ProductOption Create(
        TenantId tenantId,
        ProductId productId,
        string name,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (productId.IsEmpty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(productId));
        }

        return new ProductOption(
            ProductOptionId.New(),
            tenantId,
            productId,
            name,
            sortOrder,
            createdAtUtc,
            createdByUserId);
    }

    public void Rename(
        string name,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        var normalized =
            NormalizeName(
                name);

        var key =
            CreateNameKey(
                normalized);

        if (Name == normalized &&
            NormalizedName == key)
        {
            return;
        }

        Name =
            normalized;

        NormalizedName =
            key;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeSortOrder(
        int sortOrder,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        var validated =
            ValidateSortOrder(
                sortOrder);

        if (SortOrder == validated)
        {
            return;
        }

        SortOrder =
            validated;

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

    private void ApplyName(
        string name)
    {
        Name =
            NormalizeName(
                name);

        NormalizedName =
            CreateNameKey(
                Name);
    }

    private static string NormalizeName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Product option name is required.",
                nameof(name));
        }

        var normalized =
            name
                .Trim()
                .Normalize(
                    NormalizationForm.FormKC);

        if (normalized.Length > 100)
        {
            throw new ArgumentException(
                "Product option name cannot exceed 100 characters.",
                nameof(name));
        }

        return normalized;
    }

    private static string CreateNameKey(
        string name)
    {
        return name
            .ToLowerInvariant();
    }

    private static int ValidateSortOrder(
        int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Product option sort order cannot be negative.");
        }

        return sortOrder;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "A deleted product option cannot be modified.");
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
}