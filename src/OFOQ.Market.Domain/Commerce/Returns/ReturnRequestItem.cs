using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Returns;

public sealed class ReturnRequestItem : Entity<ReturnRequestItemId>, ITenantDataScoped
{
    private ReturnRequestItem() { }

    private ReturnRequestItem(ReturnRequestItemId id, TenantId tenantId, ReturnRequestId returnRequestId, OrderItemId orderItemId, ProductId productId, ProductVariantId productVariantId, int quantity) : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (returnRequestId.IsEmpty) throw new ArgumentException("Return request ID cannot be empty.", nameof(returnRequestId));
        if (orderItemId.IsEmpty) throw new ArgumentException("Order item ID cannot be empty.", nameof(orderItemId));
        if (productId.IsEmpty) throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        if (productVariantId.IsEmpty) throw new ArgumentException("Product variant ID cannot be empty.", nameof(productVariantId));
        if (quantity <= 0 || quantity > OrderItem.MaximumQuantity) throw new ArgumentOutOfRangeException(nameof(quantity));
        TenantId = tenantId; ReturnRequestId = returnRequestId; OrderItemId = orderItemId; ProductId = productId; ProductVariantId = productVariantId; Quantity = quantity;
    }

    public TenantId TenantId { get; private set; }
    public ReturnRequestId ReturnRequestId { get; private set; }
    public OrderItemId OrderItemId { get; private set; }
    public ProductId ProductId { get; private set; }
    public ProductVariantId ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public int RestockedQuantity { get; private set; }
    public DateTimeOffset? RestockedAtUtc { get; private set; }

    internal static ReturnRequestItem Create(TenantId tenantId, ReturnRequestId returnRequestId, OrderItemId orderItemId, ProductId productId, ProductVariantId productVariantId, int quantity)
        => new(ReturnRequestItemId.New(), tenantId, returnRequestId, orderItemId, productId, productVariantId, quantity);

    internal void MarkRestocked(DateTimeOffset at)
    {
        if (RestockedQuantity == Quantity) return;
        if (RestockedQuantity != 0) throw new InvalidOperationException("Return item has a partial restock state.");
        RestockedQuantity = Quantity;
        RestockedAtUtc = at;
    }
}
