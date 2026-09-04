using System.Text;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class ProductOptionValue :
    Entity<ProductOptionValueId>,
    ITenantDataScoped,
    IAuditable,
    ISoftDeletable
{
    private ProductOptionValue()
    {
    }

    private ProductOptionValue(
        ProductOptionValueId id,
        TenantId tenantId,
        ProductId productId,
        ProductOptionId productOptionId,
        string value,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        ProductId =
            productId;

        ProductOptionId =
            productOptionId;

        ApplyValue(
            value);

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

    public ProductOptionId ProductOptionId { get; private set; }

    public string Value { get; private set; } =
        string.Empty;

    public string NormalizedValue { get; private set; } =
        string.Empty;

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static ProductOptionValue Create(
        TenantId tenantId,
        ProductId productId,
        ProductOptionId productOptionId,
        string value,
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

        if (productOptionId.IsEmpty)
        {
            throw new ArgumentException(
                "Product option ID cannot be empty.",
                nameof(productOptionId));
        }

        return new ProductOptionValue(
            ProductOptionValueId.New(),
            tenantId,
            productId,
            productOptionId,
            value,
            sortOrder,
            createdAtUtc,
            createdByUserId);
    }

    public void Rename(
        string value,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        var normalized =
            NormalizeValue(
                value);

        var key =
            CreateValueKey(
                normalized);

        if (Value == normalized &&
            NormalizedValue == key)
        {
            return;
        }

        Value =
            normalized;

        NormalizedValue =
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

    private void ApplyValue(
        string value)
    {
        Value =
            NormalizeValue(
                value);

        NormalizedValue =
            CreateValueKey(
                Value);
    }

    private static string NormalizeValue(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "Product option value is required.",
                nameof(value));
        }

        var normalized =
            value
                .Trim()
                .Normalize(
                    NormalizationForm.FormKC);

        if (normalized.Length > 100)
        {
            throw new ArgumentException(
                "Product option value cannot exceed 100 characters.",
                nameof(value));
        }

        return normalized;
    }

    private static string CreateValueKey(
        string value)
    {
        return value
            .ToLowerInvariant();
    }

    private static int ValidateSortOrder(
        int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Product option value sort order cannot be negative.");
        }

        return sortOrder;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "A deleted product option value cannot be modified.");
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