using System.Data;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Endpoints.Commerce;

// Reporting is read-only. Every SQL join explicitly includes tenant_id;
// customer names, email addresses and telephone numbers are never returned.
public static class CouponAnalyticsEndpoints
{
    public sealed record CouponUseResult(Guid OrderId, DateTimeOffset RedeemedAtUtc,
        decimal DiscountAmount, decimal OrderAmount, string Status);
    public sealed record CouponAnalyticsResult(Guid CouponId, int UsageCount,
        int UniqueCustomers, int PaidOrderCount, int PendingOrderCount,
        int CancelledOrderCount, decimal PaidRevenue, decimal PaidDiscountTotal,
        decimal PendingRevenue, IReadOnlyList<CouponUseResult> RecentUses);

    public static IEndpointRouteBuilder MapCouponAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/tenants/{tenantId:guid}/backoffice/coupons/{id:guid}/analytics", GetAsync)
            .WithTags("Coupons").RequireAuthorization(AuthorizationPolicies.TenantBackOffice);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(Guid tenantId, Guid id, ICurrentTenant current,
        MarketDbContext db, CancellationToken ct)
    {
        if (tenantId == Guid.Empty || id == Guid.Empty || !current.IsAvailable ||
            !current.TenantId.HasValue || current.TenantId.Value.Value != tenantId)
            return Results.Forbid();

        var couponId = DiscountCouponId.From(id);
        var coupon = await db.DiscountCoupons.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == current.TenantId.Value && x.Id == couponId, ct);
        if (coupon is null) return Results.NotFound();

        // One parameterized read-only query, rather than loading every order into memory.
        // Order amount is calculated from stored order-item snapshots, not current product prices.
        const string sql = """
            SELECT r.order_id, r.customer_user_id, r.discount_amount,
                   r.redeemed_at_utc, o.status, o.shipping_amount,
                   o.discount_amount, COALESCE(items.subtotal, 0)
            FROM commerce_coupon_redemptions AS r
            INNER JOIN commerce_orders AS o
              ON o.tenant_id = r.tenant_id AND o.id = r.order_id
            LEFT JOIN LATERAL (
              SELECT SUM(i.unit_price_amount * i.quantity) AS subtotal
              FROM commerce_order_items AS i
              WHERE i.tenant_id = r.tenant_id AND i.order_id = r.order_id
            ) AS items ON TRUE
            WHERE r.tenant_id = @tenant AND r.coupon_id = @coupon
            ORDER BY r.redeemed_at_utc DESC, r.order_id
            """;

        var usage = 0;
        var unique = new HashSet<Guid>();
        var paidCount = 0;
        var pendingCount = 0;
        var cancelledCount = 0;
        decimal paidSales = 0, paidDiscount = 0, pendingSales = 0;
        var recent = new List<CouponUseResult>(40);

        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            var tenantParameter = command.CreateParameter();
            tenantParameter.ParameterName = "tenant";
            tenantParameter.Value = tenantId;
            command.Parameters.Add(tenantParameter);
            var couponParameter = command.CreateParameter();
            couponParameter.ParameterName = "coupon";
            couponParameter.Value = id;
            command.Parameters.Add(couponParameter);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var orderId = reader.GetGuid(0);
                unique.Add(reader.GetGuid(1));
                var discount = reader.GetDecimal(2);
                var timestamp = reader.GetFieldValue<DateTimeOffset>(3);
                var status = reader.GetInt32(4);
                var total = Math.Max(0m, reader.GetDecimal(7) + reader.GetDecimal(5) - reader.GetDecimal(6));
                usage++;
                switch (status)
                {
                    case 0: pendingCount++; pendingSales += total; break;
                    case 4: cancelledCount++; break;
                    case 1: case 2: case 3: case 5:
                        paidCount++; paidSales += total; paidDiscount += discount; break;
                }
                if (recent.Count < 40)
                {
                    var state = status switch
                    {
                        0 => "بانتظار الدفع", 1 => "مؤكد", 2 => "قيد التجهيز",
                        3 => "مكتمل", 4 => "ملغي", 5 => "مدفوع", _ => "حالة أخرى"
                    };
                    recent.Add(new CouponUseResult(orderId, timestamp, discount, total, state));
                }
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
        return Results.Ok(new CouponAnalyticsResult(id, usage, unique.Count, paidCount,
            pendingCount, cancelledCount, paidSales, paidDiscount, pendingSales, recent));
    }
}
