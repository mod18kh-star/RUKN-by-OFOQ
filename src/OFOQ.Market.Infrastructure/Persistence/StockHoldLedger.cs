using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Commerce.StockReservations;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL stock hold ledger. Every inventory decision uses the same variant row lock
/// and the caller's EF execution-strategy transaction; expired holds do not count.
/// No irreversible external payment side effects are permitted within these methods.
/// </summary>
internal sealed class StockHoldLedger : IStockHoldLedger
{
    private readonly MarketDbContext _db;
    public bool Enabled { get; }

    public StockHoldLedger(MarketDbContext db, IConfiguration config)
    {
        _db = db;
        Enabled = config.GetValue<bool>("StockReservations:Enabled");
    }

    private Guid TenantId => _db.HasCurrentTenant && !_db.CurrentTenantId.IsEmpty
        ? _db.CurrentTenantId.Value
        : throw new TenantScopeViolationException("Tenant context required for stock holds.");

    private NpgsqlCommand Command(string sql, params (string Name, object? Value)[] args)
    {
        var tx = _db.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction
            ?? throw new InvalidOperationException("Stock holds require an active EF PostgreSQL transaction.");
        var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
        var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    private async Task<int> Exec(string sql, CancellationToken ct, params (string, object?)[] args)
    {
        await using var cmd = Command(sql, args);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<object?> Scalar(string sql, CancellationToken ct, params (string, object?)[] args)
    {
        await using var cmd = Command(sql, args);
        return await cmd.ExecuteScalarAsync(ct);
    }

    private async Task<(bool Managed, int Quantity)> LockStockAsync(Guid variantId, CancellationToken ct)
    {
        await using var cmd = Command("""
            SELECT quantity, track_inventory, continue_selling_when_out_of_stock, is_enabled, is_deleted
            FROM catalog_product_variants WHERE tenant_id=@t AND id=@v FOR UPDATE
            """, ("t", TenantId), ("v", variantId));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct) || !reader.GetBoolean(3) || reader.GetBoolean(4))
            throw new StockHoldUnavailableException();
        return (reader.GetBoolean(1) && !reader.GetBoolean(2), reader.GetInt32(0));
    }

    private async Task<int> OtherHoldsAsync(Guid variantId, Guid? cartId, Guid? orderId,
        DateTimeOffset now, CancellationToken ct)
    {
        var result = await Scalar("""
            SELECT COALESCE(SUM(quantity),0)::integer FROM commerce_stock_holds
            WHERE tenant_id=@t AND variant_id=@v
              AND (state='Review' OR (state IN ('Cart','Order') AND expires_at_utc>@now))
              AND (@cart::uuid IS NULL OR cart_id IS DISTINCT FROM @cart::uuid)
              AND (@order::uuid IS NULL OR order_id IS DISTINCT FROM @order::uuid)
            """, ct, ("t", TenantId), ("v", variantId), ("now", now),
            ("cart", (object?)cartId ?? DBNull.Value), ("order", (object?)orderId ?? DBNull.Value));
        return Convert.ToInt32(result);
    }

    public async Task ReserveCartLineAsync(CartId cartId, ProductVariantId variantId, int quantity,
        DateTimeOffset now, CancellationToken ct)
    {
        if (!Enabled) return;
        if (quantity < 1 || quantity > CartItem.MaximumQuantity)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        var variant = variantId.Value;
        var (managed, onHand) = await LockStockAsync(variant, ct);
        if (!managed) return;
        var otherHolds = await OtherHoldsAsync(variant, cartId.Value, null, now, ct);
        if (quantity > onHand - otherHolds) throw new StockHoldUnavailableException();

        // Never extend an unexpired hold merely by refreshing a page or editing a line.
        var expiry = await Scalar("""
            SELECT expires_at_utc FROM commerce_stock_holds
            WHERE tenant_id=@t AND cart_id=@cart AND variant_id=@v FOR UPDATE
            """, ct, ("t", TenantId), ("cart", cartId.Value), ("v", variant));
        var expiresAt = StockHoldLeasePolicy.ExpiresAt(expiry as DateTimeOffset?, now);
        await Exec("""
            INSERT INTO commerce_stock_holds
              (id,tenant_id,variant_id,cart_id,order_id,quantity,state,expires_at_utc,created_at_utc,updated_at_utc)
            VALUES (@id,@t,@v,@cart,NULL,@qty,'Cart',@expiry,@now,@now)
            ON CONFLICT (tenant_id,cart_id,variant_id) WHERE cart_id IS NOT NULL
            DO UPDATE SET quantity=EXCLUDED.quantity,state='Cart',expires_at_utc=EXCLUDED.expires_at_utc,
                          updated_at_utc=EXCLUDED.updated_at_utc
            """, ct, ("id", Guid.NewGuid()), ("t", TenantId), ("v", variant),
            ("cart", cartId.Value), ("qty", quantity), ("expiry", expiresAt), ("now", now));
    }

    public async Task ReleaseCartLineAsync(CartId cartId, ProductVariantId variantId, CancellationToken ct)
    {
        if (!Enabled) return;
        await Exec("""
            UPDATE commerce_stock_holds SET cart_id=NULL,state='Released',expires_at_utc=NULL,
              updated_at_utc=now() WHERE tenant_id=@t AND cart_id=@cart AND variant_id=@v AND state='Cart'
            """, ct, ("t", TenantId), ("cart", cartId.Value), ("v", variantId.Value));
    }

    public async Task ReleaseCartAsync(CartId cartId, CancellationToken ct)
    {
        if (!Enabled) return;
        await Exec("""
            UPDATE commerce_stock_holds SET cart_id=NULL,state='Released',expires_at_utc=NULL,
              updated_at_utc=now() WHERE tenant_id=@t AND cart_id=@cart AND state='Cart'
            """, ct, ("t", TenantId), ("cart", cartId.Value));
    }

    public async Task ConvertCartToOrderAsync(Cart cart, Order order, DateTimeOffset now, CancellationToken ct)
    {
        if (!Enabled) return;
        // Caller has locked the cart. Each variant lock is acquired in UUID order.
        foreach (var item in cart.Items.OrderBy(x => x.ProductVariantId.Value))
        {
            await ReserveCartLineAsync(cart.Id, item.ProductVariantId, item.Quantity, now, ct);
            await Exec("""
                UPDATE commerce_stock_holds SET cart_id=NULL,order_id=@order,state='Order',
                   updated_at_utc=@now
                WHERE tenant_id=@t AND cart_id=@cart AND variant_id=@v AND state='Cart'
                """, ct, ("t", TenantId), ("cart", cart.Id.Value),
                ("v", item.ProductVariantId.Value), ("order", order.Id.Value), ("now", now));
        }
        // Marker distinguishes new deferred orders from legacy already-deducted orders.
        await Exec("""
            INSERT INTO commerce_stock_hold_orders(tenant_id,order_id,created_at_utc)
            VALUES (@t,@order,@now) ON CONFLICT (tenant_id,order_id) DO NOTHING
            """, ct, ("t", TenantId), ("order", order.Id.Value), ("now", now));
    }

    public async Task<bool> IsDeferredOrderAsync(OrderId orderId, CancellationToken ct)
    {
        if (!Enabled) return false;
        return (bool)(await Scalar("""
            SELECT EXISTS(SELECT 1 FROM commerce_stock_hold_orders WHERE tenant_id=@t AND order_id=@order)
            """, ct, ("t", TenantId), ("order", orderId.Value)) ?? false);
    }

    public async Task HoldOrderForReviewAsync(Order order, DateTimeOffset now, CancellationToken ct)
    {
        if (!Enabled || !await IsDeferredOrderAsync(order.Id, ct)) return;
        // Reserve the complete order atomically before accepting proof. An expired hold
        // may be reacquired ONLY if remaining stock still covers the entire order.
        foreach (var item in order.Items.OrderBy(x => x.ProductVariantId.Value))
        {
            var variant = item.ProductVariantId.Value;
            var (managed, onHand) = await LockStockAsync(variant, ct);
            if (!managed) continue;
            var others = await OtherHoldsAsync(variant, null, order.Id.Value, now, ct);
            if (item.Quantity > onHand - others) throw new StockHoldUnavailableException();
            var existing = await Scalar("""
                SELECT state FROM commerce_stock_holds WHERE tenant_id=@t AND order_id=@order AND variant_id=@v
                FOR UPDATE
                """, ct, ("t", TenantId), ("order", order.Id.Value), ("v", variant));
            if (existing is string existingState && existingState is not ("Order" or "Review" or "Released"))
                throw new StockHoldUnavailableException();
            if (existing is string)
                await Exec("""
                    UPDATE commerce_stock_holds SET quantity=@qty,state='Review',expires_at_utc=NULL,
                        updated_at_utc=@now WHERE order_id=@order AND variant_id=@v AND tenant_id=@t
                    """, ct, ("qty", item.Quantity), ("now", now), ("order", order.Id.Value),
                    ("v", variant), ("t", TenantId));
            else
                await Exec("""
                    INSERT INTO commerce_stock_holds
                      (id,tenant_id,variant_id,cart_id,order_id,quantity,state,expires_at_utc,created_at_utc,updated_at_utc)
                    VALUES (@id,@t,@v,NULL,@order,@qty,'Review',NULL,@now,@now)
                    """, ct, ("id", Guid.NewGuid()), ("t", TenantId), ("v", variant),
                    ("order", order.Id.Value), ("qty", item.Quantity), ("now", now));
        }
    }

    public async Task CaptureOrderAsync(Order order, DateTimeOffset now, Guid? actor, CancellationToken ct)
    {
        if (!Enabled || !await IsDeferredOrderAsync(order.Id, ct)) return;
        // Load existing movements explicitly: the manual-payment lock does not load them.
        await _db.Entry(order).Collection("_inventoryMovements").LoadAsync(ct);
        foreach (var item in order.Items.OrderBy(x => x.ProductVariantId.Value))
        {
            var variantId = item.ProductVariantId.Value;
            var (managed, _) = await LockStockAsync(variantId, ct);
            if (!managed) continue;
            await using var cmd = Command("""
                SELECT quantity,state,expires_at_utc FROM commerce_stock_holds
                WHERE tenant_id=@t AND order_id=@order AND variant_id=@v FOR UPDATE
                """, ("t", TenantId), ("order", order.Id.Value), ("v", variantId));
            int held; string state; DateTimeOffset? expires;
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                if (!await reader.ReadAsync(ct)) throw new StockHoldExpiredException();
                held = reader.GetInt32(0);
                state = reader.GetString(1);
                expires = reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2);
            }
            if (state == "Captured") continue; // idempotent: retry must not deduct again
            if (held != item.Quantity || state is not ("Review" or "Order") ||
                (state == "Order" && (expires is null || expires <= now)))
                throw new StockHoldExpiredException();
            var variant = await _db.ProductVariants.SingleAsync(v => v.Id == item.ProductVariantId, ct);
            await _db.Entry(variant).ReloadAsync(ct); // on-hand as of the lock, not before it
            if (variant.Inventory.TrackInventory)
            {
                if (!variant.Inventory.ContinueSellingWhenOutOfStock && variant.Inventory.Quantity < held)
                    throw new StockHoldUnavailableException();
                var before = variant.Inventory.Quantity;
                variant.DecreaseStock(held, now, actor);
                order.RecordCheckoutInventoryDeduction(item.ProductId, item.ProductVariantId,
                    before, variant.Inventory.Quantity, now, actor);
            }
            await Exec("""
                UPDATE commerce_stock_holds SET state='Captured',expires_at_utc=NULL,updated_at_utc=@now
                WHERE tenant_id=@t AND order_id=@order AND variant_id=@v AND state=@state
                """, ct, ("t", TenantId), ("order", order.Id.Value), ("v", variantId),
                ("state", state), ("now", now));
        }
        // Tracked oversell-enabled variants are intentionally not reserved, but their
        // on-hand physical quantity still decreases once upon successful payment.
        foreach (var item in order.Items.Where(x => x.Quantity > 0).OrderBy(x => x.ProductVariantId.Value))
        {
            var variant = await _db.ProductVariants.SingleAsync(v => v.Id == item.ProductVariantId, ct);
            if (!variant.Inventory.TrackInventory || !variant.Inventory.ContinueSellingWhenOutOfStock)
                continue;
            await LockStockAsync(item.ProductVariantId.Value, ct);
            await _db.Entry(variant).ReloadAsync(ct);
            if (order.InventoryMovements.Any(m => m.ProductVariantId == item.ProductVariantId &&
                m.Type == InventoryMovementType.CheckoutDeduction)) continue;
            var before = variant.Inventory.Quantity;
            variant.DecreaseStock(item.Quantity, now, actor);
            order.RecordCheckoutInventoryDeduction(item.ProductId, item.ProductVariantId,
                before, variant.Inventory.Quantity, now, actor);
        }
    }

    // Merchants cannot reduce on-hand below customer-owned unexpired reservations.
    // The caller must execute the UPDATE under this SAME EF transaction.
    public async Task GuardInventoryAdjustmentAsync(ProductVariantId variantId,
        int proposedPhysicalQuantity, bool trackInventory, bool continueSelling,
        DateTimeOffset now, CancellationToken ct)
    {
        if (!Enabled) return;
        var (managed, _) = await LockStockAsync(variantId.Value, ct);
        var active = await OtherHoldsAsync(variantId.Value, null, null, now, ct);
        if (active > 0 && (!trackInventory || continueSelling || !managed || proposedPhysicalQuantity < active))
            throw new StockHoldUnavailableException();
    }

    public async Task ReleaseOrderAsync(OrderId orderId, CancellationToken ct)
    {
        if (!Enabled || !await IsDeferredOrderAsync(orderId, ct)) return;
        await Exec("""
            UPDATE commerce_stock_holds SET state='Released',expires_at_utc=NULL,updated_at_utc=now()
            WHERE tenant_id=@t AND order_id=@order AND state IN ('Order','Review')
            """, ct, ("t", TenantId), ("order", orderId.Value));
    }
}
