using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog.Attributes;

public sealed class ProductAttributeValue :
    Entity<ProductAttributeValueId>,
    ITenantDataScoped,
    IAuditable
{
    private ProductAttributeValue()
    {
    }

    private ProductAttributeValue(
        ProductAttributeValueId id,
        TenantId tenantId,
        ProductId productId,
        string key,
        string value,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        ProductId =
            productId;

        Key =
            NormalizeKey(
                key);

        Value =
            NormalizeStoredValue(
                value);

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public ProductId ProductId { get; private set; }

    public string Key { get; private set; } =
        string.Empty;

    public string Value { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static ProductAttributeValue Create(
        TenantId tenantId,
        ProductId productId,
        string key,
        string value,
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

        return new ProductAttributeValue(
            ProductAttributeValueId.New(),
            tenantId,
            productId,
            key,
            value,
            createdAtUtc,
            createdByUserId);
    }

    public void ChangeValue(
        string value,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var normalized =
            NormalizeStoredValue(
                value);

        if (Value ==
            normalized)
        {
            return;
        }

        Value =
            normalized;

        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private static string NormalizeKey(
        string key)
    {
        if (string.IsNullOrWhiteSpace(
                key))
        {
            throw new ArgumentException(
                "Product attribute key is required.",
                nameof(key));
        }

        var normalized =
            key.Trim()
                .ToLowerInvariant();

        if (normalized.Length >
            80)
        {
            throw new ArgumentException(
                "Product attribute key cannot exceed 80 characters.",
                nameof(key));
        }

        return normalized;
    }

    private static string NormalizeStoredValue(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "Product attribute value is required.",
                nameof(value));
        }

        var normalized =
            value.Trim();

        if (normalized.Length >
            500)
        {
            throw new ArgumentException(
                "Product attribute value cannot exceed 500 characters.",
                nameof(value));
        }

        return normalized;
    }
}