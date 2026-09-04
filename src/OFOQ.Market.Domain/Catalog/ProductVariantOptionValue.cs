using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class ProductVariantOptionValue :
    Entity<ProductVariantOptionValueId>,
    ITenantDataScoped,
    IAuditable,
    ISoftDeletable
{
    private ProductVariantOptionValue()
    {
    }

    private ProductVariantOptionValue(
        ProductVariantOptionValueId id,
        TenantId tenantId,
        ProductId productId,
        ProductVariantId productVariantId,
        ProductOptionId productOptionId,
        ProductOptionValueId productOptionValueId,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        ProductId =
            productId;

        ProductVariantId =
            productVariantId;

        ProductOptionId =
            productOptionId;

        ProductOptionValueId =
            productOptionValueId;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public ProductId ProductId { get; private set; }

    public ProductVariantId ProductVariantId { get; private set; }

    public ProductOptionId ProductOptionId { get; private set; }

    public ProductOptionValueId ProductOptionValueId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static ProductVariantOptionValue Create(
        TenantId tenantId,
        ProductId productId,
        ProductVariantId productVariantId,
        ProductOptionId productOptionId,
        ProductOptionValueId productOptionValueId,
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

        if (productVariantId.IsEmpty)
        {
            throw new ArgumentException(
                "Product variant ID cannot be empty.",
                nameof(productVariantId));
        }

        if (productOptionId.IsEmpty)
        {
            throw new ArgumentException(
                "Product option ID cannot be empty.",
                nameof(productOptionId));
        }

        if (productOptionValueId.IsEmpty)
        {
            throw new ArgumentException(
                "Product option value ID cannot be empty.",
                nameof(productOptionValueId));
        }

        return new ProductVariantOptionValue(
            ProductVariantOptionValueId.New(),
            tenantId,
            productId,
            productVariantId,
            productOptionId,
            productOptionValueId,
            createdAtUtc,
            createdByUserId);
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

        UpdatedAtUtc =
            deletedAtUtc;

        UpdatedByUserId =
            deletedByUserId;
    }
}