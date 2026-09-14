using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Orders;

public sealed class InventoryMovement :
    Entity<InventoryMovementId>,
    ITenantDataScoped
{
    private InventoryMovement()
    {
    }

    private InventoryMovement(
        InventoryMovementId id,
        TenantId tenantId,
        OrderId orderId,
        ProductId productId,
        ProductVariantId productVariantId,
        InventoryMovementType type,
        int quantityBefore,
        int quantityAfter,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        OrderId =
            orderId;

        ProductId =
            productId;

        ProductVariantId =
            productVariantId;

        Type =
            type;

        QuantityBefore =
            quantityBefore;

        QuantityAfter =
            quantityAfter;

        QuantityDelta =
            checked(
                quantityAfter -
                quantityBefore);

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public OrderId OrderId { get; private set; }

    public ProductId ProductId { get; private set; }

    public ProductVariantId ProductVariantId { get; private set; }

    public InventoryMovementType Type { get; private set; }

    public int QuantityBefore { get; private set; }

    public int QuantityAfter { get; private set; }

    public int QuantityDelta { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    internal static InventoryMovement Create(
        TenantId tenantId,
        OrderId orderId,
        ProductId productId,
        ProductVariantId productVariantId,
        InventoryMovementType type,
        int quantityBefore,
        int quantityAfter,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (orderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(orderId));
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

        if (quantityBefore < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityBefore));
        }

        if (quantityAfter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityAfter));
        }

        if (quantityAfter ==
            quantityBefore)
        {
            throw new ArgumentException(
                "Inventory movement must change quantity.");
        }

        return new InventoryMovement(
            InventoryMovementId.New(),
            tenantId,
            orderId,
            productId,
            productVariantId,
            type,
            quantityBefore,
            quantityAfter,
            createdAtUtc,
            createdByUserId);
    }
}