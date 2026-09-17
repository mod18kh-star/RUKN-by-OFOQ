using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class ProductRelation :
    Entity<ProductRelationId>,
    ITenantDataScoped,
    IAuditable
{
    private ProductRelation()
    {
    }

    private ProductRelation(
        ProductRelationId id,
        TenantId tenantId,
        ProductId sourceProductId,
        ProductId targetProductId,
        ProductRelationType type,
        int sortOrder,
        bool isVisible,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        SourceProductId =
            sourceProductId;

        TargetProductId =
            targetProductId;

        Type =
            NormalizeType(
                type);

        SortOrder =
            NormalizeSortOrder(
                sortOrder);

        IsVisible =
            isVisible;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public ProductId SourceProductId { get; private set; }

    public ProductId TargetProductId { get; private set; }

    public ProductRelationType Type { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsVisible { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static ProductRelation Create(
        TenantId tenantId,
        ProductId sourceProductId,
        ProductId targetProductId,
        ProductRelationType type,
        int sortOrder,
        bool isVisible,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (sourceProductId.IsEmpty)
        {
            throw new ArgumentException(
                "Source product ID cannot be empty.",
                nameof(sourceProductId));
        }

        if (targetProductId.IsEmpty)
        {
            throw new ArgumentException(
                "Target product ID cannot be empty.",
                nameof(targetProductId));
        }

        if (sourceProductId ==
            targetProductId)
        {
            throw new ArgumentException(
                "A product cannot be related to itself.",
                nameof(targetProductId));
        }

        return new ProductRelation(
            ProductRelationId.New(),
            tenantId,
            sourceProductId,
            targetProductId,
            type,
            sortOrder,
            isVisible,
            createdAtUtc,
            createdByUserId);
    }

    private static ProductRelationType NormalizeType(
        ProductRelationType type)
    {
        if (!Enum.IsDefined(
                type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Unsupported product relation type.");
        }

        return type;
    }

    private static int NormalizeSortOrder(
        int sortOrder)
    {
        if (sortOrder <
            0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Product relation sort order cannot be negative.");
        }

        return sortOrder;
    }
}
