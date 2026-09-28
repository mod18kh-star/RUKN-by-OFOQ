using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Common.Persistence;

// Stage 4D: All methods run INSIDE the caller's retriable database transaction.
// Legacy orders have no stock-hold marker; their checkout deductions remain untouched.
public interface IStockHoldLedger
{
    bool Enabled { get; }
    Task ReserveCartLineAsync(CartId cartId, ProductVariantId variantId, int quantity,
        DateTimeOffset now, CancellationToken cancellationToken);
    Task ReleaseCartLineAsync(CartId cartId, ProductVariantId variantId, CancellationToken cancellationToken);
    Task ReleaseCartAsync(CartId cartId, CancellationToken cancellationToken);
    Task ConvertCartToOrderAsync(Cart cart, Order order, DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<bool> IsDeferredOrderAsync(OrderId orderId, CancellationToken cancellationToken);
    Task HoldOrderForReviewAsync(Order order, DateTimeOffset now, CancellationToken cancellationToken);
    Task CaptureOrderAsync(Order order, DateTimeOffset now, Guid? actor,
        CancellationToken cancellationToken);
    Task ReleaseOrderAsync(OrderId orderId, CancellationToken cancellationToken);
    Task GuardInventoryAdjustmentAsync(ProductVariantId variantId, int proposedPhysicalQuantity,
        bool trackInventory, bool continueSelling, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class StockHoldUnavailableException : Exception
{
    public StockHoldUnavailableException()
        : base("المنتج لم يعد متاحًا بالكمية المطلوبة. حدّث السلة قبل إتمام الشراء.") { }
}

public sealed class StockHoldExpiredException : Exception
{
    public StockHoldExpiredException()
        : base("انتهت مهلة حجز المنتج. يرجى مراجعة توفر المنتجات قبل استكمال الدفع.") { }
}
