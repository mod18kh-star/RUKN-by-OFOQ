using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Endpoints.Commerce;

// Stage 2C: merchant-paid manual transfers only; no payment gateway and no RUKN custody of funds.
public static class ManualOrderPaymentEndpoints
{
    private const int MaxReceiptBytes = 1024 * 1024;
    private static Guid Actor(HttpContext http) =>
        Guid.TryParse(http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var user) ? user : Guid.Empty;
    private static bool InScope(Guid tenantId, ICurrentTenant current) =>
        tenantId != Guid.Empty && current.IsAvailable && current.TenantId.HasValue &&
        current.TenantId.Value.Value == tenantId;
    public sealed record SelectManualMethodRequest(Guid AccountId, string CustomerPhone);
    public sealed record ReviewManualReceiptRequest(bool Approve, string? Reason);
    public sealed record RestoreCartRequest(bool ConfirmNoTransfer);
    public sealed record ManualMethodSummary(Guid Id, string Kind, string Name, string MaskedReference);
    public sealed record ManualPaymentCustomerResult(Guid OrderId, string Status, Guid AccountId,
        string AccountName, string AccountKind, string? BankName, string? AccountHolder,
        string? Iban, string? AccountNumber, string? WalletProvider, string? WalletNumber,
        string? TransferLink, bool HasQr, decimal Amount, string Currency, string? RejectionReason,
        DateTimeOffset? LastSubmittedAtUtc);
    public sealed record ManualPaymentMerchantResult(Guid OrderId, string Status, string CustomerName,
        string CustomerPhone, Guid CustomerUserId, Guid AccountId, string AccountName,
        decimal Amount, string Currency, Guid? ReceiptId, string? ReceiptStatus,
        string? RejectionReason, DateTimeOffset UpdatedAtUtc);

    public static IEndpointRouteBuilder MapManualOrderPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var customer = endpoints.MapGroup("/api/tenants/{tenantId:guid}/payments/manual-checkout")
            .WithTags("Manual Checkout").RequireAuthorization();
        customer.MapGet("/methods", GetMethodsAsync);
        customer.MapGet("/orders/mine", GetMineAsync);
        customer.MapPost("/orders/{orderId:guid}/select", SelectMethodAsync);
        customer.MapGet("/orders/{orderId:guid}", GetCustomerOrderAsync);
        customer.MapGet("/orders/{orderId:guid}/qr", GetCustomerQrAsync);
        customer.MapPost("/orders/{orderId:guid}/receipt", UploadReceiptAsync);
        customer.MapGet("/orders/{orderId:guid}/receipt", GetCustomerReceiptAsync);
        customer.MapPost("/orders/{orderId:guid}/restore-cart", RestoreUnpaidCartAsync);
        var merchant = endpoints.MapGroup("/api/tenants/{tenantId:guid}/backoffice/manual-payments")
            .WithTags("Manual Payment Review")
            .RequireAuthorization(AuthorizationPolicies.TenantPaymentAdministration);
        merchant.MapGet("/", GetMerchantPaymentsAsync);
        merchant.MapGet("/{orderId:guid}/receipt", GetMerchantReceiptAsync);
        merchant.MapPost("/{orderId:guid}/review", ReviewAsync);
        endpoints.MapManualCheckoutDraftEndpoints();
        return endpoints;
    }

    private static IResult Invalid(string message) => Results.BadRequest(new { message });
    private static IResult Conflict(string message) => Results.Conflict(new { message });
    private static bool Owns(Order? order, Guid actor) => order is not null &&
        actor != Guid.Empty && order.CustomerUserId.Value == actor;
    private static string? Field(IReadOnlyDictionary<string, string> fields, string name) =>
        fields.TryGetValue(name, out var value) ? value : null;

    // All SELECT ... FOR UPDATE operations must execute inside the configured Npgsql
    // retry strategy. Each retry starts with a clean tracker and a new transaction.
    // Do not perform external side effects inside this callback.
    private static async Task<IResult> ExecutePaymentTransactionAsync(
        MarketDbContext db, Func<Task<IResult>> operation, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Mirrors the existing EfTransactionExecutor pattern in this project.
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var result = await operation();
            await tx.CommitAsync(ct);
            return result;
        });
    }

    private static async Task<Order?> LockOrderAsync(MarketDbContext db, Guid tenantId, Guid orderId, CancellationToken ct)
    {
        var order = await db.Orders.FromSqlInterpolated(
            $"SELECT * FROM commerce_orders WHERE tenant_id = {tenantId} AND id = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

        if (order is not null)
        {
            // TotalAmount is calculated from the order items.
            // Explicitly load them before validating the payment amount.
            await db.Entry(order)
                .Collection("_items")
                .LoadAsync(ct);
        }

        return order;
    }
    private static async Task<ManualOrderPayment?> LockManualAsync(MarketDbContext db, Guid tenantId,
        Guid orderId, CancellationToken ct) =>
        await db.Set<ManualOrderPayment>().FromSqlInterpolated(
            $"SELECT * FROM commerce_manual_order_payments WHERE tenant_id = {tenantId} AND order_id = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
    private static async Task<ManualPaymentReceipt?> LatestAsync(MarketDbContext db, Guid paymentId,
        CancellationToken ct) => await db.Set<ManualPaymentReceipt>()
        .Where(r => r.ManualPaymentId == paymentId)
        .OrderByDescending(r => r.SubmittedAtUtc).ThenByDescending(r => r.Id)
        .FirstOrDefaultAsync(ct);

    // Return a payment-free order to the active cart in a single transaction.
    // Legacy orders already deducted inventory; deferred orders only own a hold.
    // Never restore when a customer might have transferred funds or a provider attempt exists.
    private static async Task<IResult> RestoreUnpaidCartAsync(
        Guid tenantId, Guid orderId, RestoreCartRequest request,
        ICurrentTenant current, MarketDbContext db, IStockHoldLedger holds, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http);
        if (actor == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty || request is null || !request.ConfirmNoTransfer)
            return Invalid("أكد أنك لم تُحوّل أي مبلغ لهذا الطلب قبل إعادة المنتجات إلى السلة.");

        try
        {
            return await ExecutePaymentTransactionAsync(db, async () =>
            {
                var order = await LockOrderAsync(db, tenantId, orderId, ct);
                if (!Owns(order, actor)) return Results.NotFound();
                if (order!.Status != OrderStatus.Pending)
                    return Conflict("لا يمكن تعديل طلب تم تأكيده أو إلغاؤه. راجع حالة الطلب أولاً.");

                var manual = await LockManualAsync(db, tenantId, orderId, ct);
                if (manual is not null &&
                    (manual.CustomerUserId != actor || manual.Status != "AwaitingReceipt"))
                    return Conflict("لا يمكن تعديل الطلب بعد إرسال إيصال أو بدء مراجعة الدفع.");

                // A receipt or electronic attempt may exist even when the manual status is old.
                // Never cancel an order that could have money in flight.
                if (await db.Set<ManualPaymentReceipt>().AnyAsync(r => r.OrderId == order.Id, ct) ||
                    await db.Payments.AnyAsync(p => p.OrderId == order.Id, ct))
                    return Conflict("يوجد إثبات أو محاولة دفع لهذا الطلب. تواصل مع المتجر قبل تغييره.");

                var cart = await db.Carts.FromSqlInterpolated(
                    $"SELECT * FROM commerce_carts WHERE tenant_id = {tenantId} AND customer_user_id = {actor} AND status = 0 FOR UPDATE")
                    .SingleOrDefaultAsync(ct);
                var now = DateTimeOffset.UtcNow;
                if (cart is null)
                {
                    cart = Cart.Create(current.TenantId!.Value, UserId.From(actor), now, actor);
                    db.Carts.Add(cart);
                }
                else
                {
                    await db.Entry(cart).Collection("_items").LoadAsync(ct);
                    if (cart.Id == order.SourceCartId)
                    {
                        // The checkout cart is intentionally visible during deferred
                        // payment. An explicit, no-transfer edit closes that snapshot
                        // before creating an editable cart; keep the old order history.
                        cart.MarkConverted(now, actor);
                        await db.SaveChangesAsync(ct);
                        cart = Cart.Create(current.TenantId!.Value, UserId.From(actor), now, actor);
                        db.Carts.Add(cart);
                    }
                }

                await db.Entry(order).Collection("_inventoryMovements").LoadAsync(ct);
                var deductions = order.InventoryMovements
                    .Where(m => m.Type == InventoryMovementType.CheckoutDeduction && m.QuantityDelta < 0)
                    .ToArray();

                // Lock in deterministic order and do not change anything if required stock is missing.
                var variants = new Dictionary<ProductVariantId, ProductVariant>();
                foreach (var variantId in deductions.Select(m => m.ProductVariantId).Distinct()
                    .OrderBy(id => id.Value))
                {
                    var variant = await db.ProductVariants.FromSqlInterpolated(
                        $"SELECT * FROM catalog_product_variants WHERE tenant_id = {tenantId} AND id = {variantId.Value} FOR UPDATE")
                        .SingleOrDefaultAsync(ct);
                    if (variant is null)
                        return Conflict("تعذر إعادة المخزون لهذا الطلب. تواصل مع إدارة المتجر.");
                    variants.Add(variantId, variant);
                }


                order.Cancel("أعاد العميل الطلب غير المدفوع إلى السلة للتعديل.", now, actor);
                foreach (var deduction in deductions)
                {
                    var quantity = order.GetCancellationRestockQuantity(deduction.ProductVariantId);
                    if (quantity <= 0) continue;
                    var variant = variants[deduction.ProductVariantId];
                    var before = variant.Inventory.Quantity;
                    variant.RestoreStockFromOrderCancellation(quantity, now, actor);
                    order.RecordCancellationInventoryRestock(deduction.ProductId,
                        deduction.ProductVariantId, before, variant.Inventory.Quantity, now, actor);
                }

                await holds.ReleaseOrderAsync(order.Id, ct);
                // The user can edit the returned lines even when the 15-minute stock hold
                // cannot be reacquired. Checkout will revalidate availability atomically.
                foreach (var item in order.Items)
                {
                    cart.AddItem(item.ProductId, item.ProductVariantId,
                        item.UnitPrice, item.Quantity, now, actor);
                    var line = cart.Items.Single(x => x.ProductVariantId == item.ProductVariantId);
                    try
                    {
                        await holds.ReserveCartLineAsync(cart.Id, item.ProductVariantId,
                            line.Quantity, now, ct);
                    }
                    catch (StockHoldUnavailableException)
                    {
                        // Restore editable cart contents without promising unowned stock.
                    }
                }

                if (manual is not null) manual.CancelBeforeReceipt(now);
                // Checkout recorded the coupon before payment; a safely cancelled order
                // must not consume the coupon quota permanently.
                var redemptions = await db.CouponRedemptions
                    .Where(r => r.OrderId == order.Id).ToArrayAsync(ct);
                db.CouponRedemptions.RemoveRange(redemptions);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { restoredCartId = cart.Id.Value, cancelledOrderId = orderId });
            }, ct);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Conflict("تعذر جمع الكميات في السلة. احذف بعض المنتجات من السلة الحالية ثم حاول مجددًا.");
        }
        catch (InvalidOperationException)
        {
            return Conflict("تعذر دمج محتويات الطلب في السلة الحالية. راجع المنتجات والعملات وحاول مجددًا.");
        }
    }

    private static async Task<IResult> GetMethodsAsync(Guid tenantId, ICurrentTenant current,
        MarketDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        if (Actor(http) == Guid.Empty) return Results.Unauthorized();
        var rows = await db.Set<MerchantManualPaymentAccount>().AsNoTracking()
            .Where(x => x.IsEnabled).OrderBy(x => x.DisplayName).ToArrayAsync(ct);
        return Results.Ok(rows.Select(x => new ManualMethodSummary(x.Id, x.Kind, x.DisplayName, x.MaskedReference)));
    }

    // This is the customer's own order history, not the merchant's order queue.
    // A checkout creates an immutable pending order before any transfer is made.
    // Exposing its item snapshots here prevents an unpaid order from appearing
    // lost when checkout converts the source cart. Do NOT create another order.
    private static async Task<IResult> GetMineAsync(Guid tenantId, ICurrentTenant current,
        MarketDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http);
        if (actor == Guid.Empty) return Results.Unauthorized();
        var scopedTenant = current.TenantId!.Value;

        var orders = await db.Orders.AsNoTracking()
            .Include("_items")
            .Where(o => o.TenantId == scopedTenant &&
                EF.Property<Guid>(o, "_customerUserId") == actor)
            .OrderByDescending(o => o.CreatedAtUtc).Take(100).ToArrayAsync(ct);

        var payments = await db.Set<ManualOrderPayment>().AsNoTracking()
            .Where(p => p.CustomerUserId == actor)
            .OrderByDescending(p => p.CreatedAtUtc).Take(100).ToArrayAsync(ct);
        var byOrder = payments.ToDictionary(p => p.OrderId.Value);
        var result = new List<object>(orders.Length);
        foreach (var order in orders)
        {
            byOrder.TryGetValue(order.Id.Value, out var manual);
            var receipt = manual is not null && manual.Status == "Rejected"
                ? await LatestAsync(db, manual.Id, ct)
                : null;
            var status = order.Status == OrderStatus.Cancelled ? "Cancelled"
                : manual?.Status ?? (order.Status == OrderStatus.Pending ? "AwaitingMethod" : "Approved");
            result.Add(new
            {
                orderId = order.Id.Value,
                status,
                orderStatus = order.Status.ToString(),
                fulfillmentStatus = order.FulfillmentStatus.ToString(),
                amount = order.TotalAmount,
                currency = order.Currency.Value,
                rejectionReason = receipt?.RejectionReason,
                order.CreatedAtUtc,
                items = order.Items.Select(item => new
                {
                    productId = item.ProductId.Value,
                    productName = item.ProductName,
                    variantName = item.VariantName,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice.Amount,
                    lineTotal = item.LineTotal
                }).ToArray()
            });
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> SelectMethodAsync(Guid tenantId, Guid orderId, SelectManualMethodRequest request,
        ICurrentTenant current, MarketDbContext db, IPaymentProviderCredentialProtector protector,
        HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty || request.AccountId == Guid.Empty) return Invalid("بيانات الطلب أو وسيلة الدفع غير صحيحة.");
        var phone = (request.CustomerPhone ?? string.Empty).Trim();
        if (phone.Length is < 7 or > 40 || phone.Count(char.IsDigit) < 7 ||
            phone.Any(ch => !(char.IsDigit(ch) || ch is '+' or '-' or ' ' or '(' or ')')))
            return Invalid("أدخل رقم تواصل صحيحًا لمتابعة الطلب.");
        return await ExecutePaymentTransactionAsync(db, async () =>
        {
            var order = await LockOrderAsync(db, tenantId, orderId, ct);
            if (!Owns(order, actor)) return Results.NotFound();
            var prior = await LockManualAsync(db, tenantId, orderId, ct);
            if (prior is not null)
            {
                if (prior.AccountId != request.AccountId) return Conflict("تم تثبيت وسيلة دفع أخرى لهذا الطلب.");
                return Results.Ok(await CustomerResultAsync(db, protector, prior, ct));
            }
            if (order!.Status != OrderStatus.Pending || order.TotalAmount <= 0 ||
                await db.Payments.AnyAsync(p => p.OrderId == order.Id, ct))
                return Conflict("الطلب غير متاح لبدء تحويل يدوي أو له عملية دفع سابقة.");
            var account = await db.Set<MerchantManualPaymentAccount>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == request.AccountId && x.IsEnabled, ct);
            if (account is null) return Conflict("وسيلة الدفع هذه متوقفة أو غير متاحة.");
            var data = protector.Unprotect(current.TenantId!.Value,
                TenantPaymentProviderAccountId.From(account.Id), account.ProtectedDetails);
            var paymentId = Guid.NewGuid();
            var snapshot = protector.Protect(current.TenantId.Value,
                TenantPaymentProviderAccountId.From(paymentId), PaymentProviderCredentialPayload.Create(data.Values));
            var payment = ManualOrderPayment.Create(current.TenantId.Value, order.Id, actor, paymentId,
                account.Id, account.DisplayName, account.Kind, snapshot,
                order.TotalAmount, order.Currency.Value, phone, DateTimeOffset.UtcNow);
            db.Set<ManualOrderPayment>().Add(payment);
            await db.SaveChangesAsync(ct);
            return Results.Ok(await CustomerResultAsync(db, protector, payment, ct));
        }, ct);
    }

    private static async Task<ManualPaymentCustomerResult> CustomerResultAsync(MarketDbContext db,
        IPaymentProviderCredentialProtector protector, ManualOrderPayment payment, CancellationToken ct)
    {
        var fields = protector.Unprotect(payment.TenantId,
            TenantPaymentProviderAccountId.From(payment.Id), payment.ProtectedAccountSnapshot).Values;
        var latest = await LatestAsync(db, payment.Id, ct);
        return new ManualPaymentCustomerResult(payment.OrderId.Value, payment.Status, payment.AccountId,
            payment.AccountName, payment.AccountKind, Field(fields, "bankName"), Field(fields, "accountHolder"),
            Field(fields, "iban"), Field(fields, "accountNumber"), Field(fields, "walletProvider"),
            Field(fields, "walletNumber"), Field(fields, "transferLink"), fields.ContainsKey("qr"),
            payment.Amount, payment.Currency, latest?.RejectionReason, latest?.SubmittedAtUtc);
    }

    private static async Task<IResult> GetCustomerOrderAsync(Guid tenantId, Guid orderId,
        ICurrentTenant current, MarketDbContext db, IPaymentProviderCredentialProtector protector,
        HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty) return Results.NotFound();
        var row = await db.Set<ManualOrderPayment>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrderId == OrderId.From(orderId) && p.CustomerUserId == actor, ct);
        return row is null ? Results.NotFound() : Results.Ok(await CustomerResultAsync(db, protector, row, ct));
    }
    private static async Task<IResult> GetCustomerQrAsync(Guid tenantId, Guid orderId,
        ICurrentTenant current, MarketDbContext db, IPaymentProviderCredentialProtector protector,
        HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty) return Results.NotFound();
        var row = await db.Set<ManualOrderPayment>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrderId == OrderId.From(orderId) && p.CustomerUserId == actor, ct);
        if (row is null) return Results.NotFound();
        var data = protector.Unprotect(row.TenantId,
            TenantPaymentProviderAccountId.From(row.Id), row.ProtectedAccountSnapshot).Values;
        if (!data.TryGetValue("qr", out var encoded)) return Results.NotFound();
        var bytes = Convert.FromBase64String(encoded);
        if (bytes.Length is < 24 or > 40960) return Results.NotFound();
        var png = bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
        var jpeg = bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217;
        if (!png && !jpeg) return Results.NotFound();
        NoStore(http);
        return Results.File(bytes, png ? "image/png" : "image/jpeg");
    }

    private static void NoStore(HttpContext http)
    {
        http.Response.Headers["Cache-Control"] = "private, no-store, max-age=0";
        http.Response.Headers["X-Content-Type-Options"] = "nosniff";
        http.Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
    }
    private static bool ValidImage(ReadOnlySpan<byte> bytes, out string mime)
    {
        mime = "";
        if (bytes.Length is < 24 or > MaxReceiptBytes) return false;
        if (bytes[..8].SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) &&
            bytes.Slice(12, 4).SequenceEqual("IHDR"u8))
        {
            var width = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4));
            var height = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4));
            if (width is < 1 or > 8192 || height is < 1 or > 8192) return false;
            mime = "image/png"; return true;
        }
        if (bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 &&
            bytes[^2] == 255 && bytes[^1] == 217)
        {
            mime = "image/jpeg"; return true;
        }
        return false;
    }

    private static async Task<IResult> UploadReceiptAsync(Guid tenantId, Guid orderId, ICurrentTenant current,
        MarketDbContext db, IConfiguration configuration, IStockHoldLedger holds, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty || !http.Request.HasFormContentType) return Invalid("يجب إرفاق صورة الإيصال.");
        if (http.Request.ContentLength.HasValue && http.Request.ContentLength.Value > MaxReceiptBytes + 65536)
            return Results.StatusCode(413);
        var limit = http.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false }) limit.MaxRequestBodySize = MaxReceiptBytes + 65536;
        var form = await http.Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("receipt");
        if (file is null || file.Length is < 24 or > MaxReceiptBytes || form.Files.Count != 1)
            return Invalid("يجب رفع صورة PNG أو JPEG بحجم لا يتجاوز 1 MB.");
        var reference = form["transferReference"].ToString().Trim();
        if (reference.Length > 100 || reference.Any(char.IsControl)) return Invalid("مرجع التحويل غير صالح.");
        var content = new byte[(int)file.Length];
        await using (var stream = file.OpenReadStream())
        {
            var read = 0;
            while (read < content.Length)
            {
                var next = await stream.ReadAsync(content.AsMemory(read), ct);
                if (next == 0) return Invalid("تعذر قراءة صورة الإيصال.");
                read += next;
            }
        }
        try
        {
            if (!ValidImage(content, out var mime)) return Invalid("صيغة الإيصال غير مقبولة؛ استخدم PNG أو JPEG.");
            return await ExecutePaymentTransactionAsync(db, async () =>
            {
                var order = await LockOrderAsync(db, tenantId, orderId, ct);
                if (!Owns(order, actor)) return Results.NotFound();
                if (order!.Status != OrderStatus.Pending) return Conflict("لا يمكن إرسال إيصال لهذا الطلب في حالته الحالية.");
                var payment = await LockManualAsync(db, tenantId, orderId, ct);
                if (payment is null || payment.CustomerUserId != actor) return Results.NotFound();
                if (payment.Status is not ("AwaitingReceipt" or "Rejected"))
                    return Conflict("الإيصال الحالي بانتظار المراجعة أو تم اعتماد الدفع بالفعل.");
                // Revalidate and freeze ALL variant holds before accepting proof.
                // On failure, the enclosing transaction rolls back, including any reacquired holds.
                await holds.HoldOrderForReviewAsync(order, DateTimeOffset.UtcNow, ct);
                var receiptId = Guid.NewGuid();
                var encrypted = ManualReceiptCrypto.Protect(configuration, tenantId, orderId, receiptId, content);
                var receipt = ManualPaymentReceipt.Create(current.TenantId!.Value, payment.Id, order.Id,
                    actor, mime, encrypted.Ciphertext, encrypted.Nonce, encrypted.Tag,
                    reference.Length == 0 ? null : reference, DateTimeOffset.UtcNow, receiptId);
                db.Set<ManualPaymentReceipt>().Add(receipt);
                var submittedAt = DateTimeOffset.UtcNow;
                payment.Submit(submittedAt);
                // Legacy pending checkouts also finish their cart at the receipt boundary,
                // not at merchant approval. A new editable cart may then be created.
                var sourceCart = await db.Carts.FromSqlInterpolated(
                    $"SELECT * FROM commerce_carts WHERE tenant_id = {tenantId} AND id = {order.SourceCartId.Value} AND customer_user_id = {actor} FOR UPDATE")
                    .SingleOrDefaultAsync(ct);
                if (sourceCart?.Status == CartStatus.Active)
                    sourceCart.MarkConverted(submittedAt, actor);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { orderId, receiptId, status = "PendingReview" });
            }, ct);
        }
        catch (StockHoldUnavailableException)
        {
            return Conflict("المنتج لم يعد متاحًا بالكمية المطلوبة. تواصل مع المتجر إذا كنت قد حولت المبلغ بالفعل.");
        }
        finally { CryptographicOperations.ZeroMemory(content); }
    }
    private static async Task<IResult> GetCustomerReceiptAsync(Guid tenantId, Guid orderId,
        ICurrentTenant current, MarketDbContext db, IConfiguration config, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty) return Results.NotFound();
        var pay = await db.Set<ManualOrderPayment>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrderId == OrderId.From(orderId) && p.CustomerUserId == actor, ct);
        return pay is null ? Results.NotFound() : await ReceiptFileAsync(tenantId, pay.Id, db, config, http, ct);
    }

    private static async Task<IResult> GetMerchantPaymentsAsync(Guid tenantId, ICurrentTenant current,
        MarketDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        if (Actor(http) == Guid.Empty) return Results.Unauthorized();
        var payments = await db.Set<ManualOrderPayment>().AsNoTracking()
            // Selecting a payment account is not an order ready for merchant review.
            .Where(p => p.Status != "AwaitingReceipt")
            .OrderByDescending(p => p.UpdatedAtUtc).Take(100).ToArrayAsync(ct);
        var result = new List<ManualPaymentMerchantResult>(payments.Length);
        foreach (var p in payments)
        {
            var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == p.OrderId, ct);
            if (order is null || order.Status == OrderStatus.Cancelled) continue;
            var profile = await db.CustomerProfiles.AsNoTracking()
                .SingleOrDefaultAsync(x => x.UserId == UserId.From(p.CustomerUserId), ct);
            var latest = await LatestAsync(db, p.Id, ct);
            result.Add(new ManualPaymentMerchantResult(p.OrderId.Value, p.Status,
                profile?.DisplayName ?? "العميل", p.CustomerPhone, p.CustomerUserId, p.AccountId,
                p.AccountName, p.Amount, p.Currency, latest?.Id, latest?.ReviewStatus,
                latest?.RejectionReason, p.UpdatedAtUtc));
        }
        return Results.Ok(result);
    }
    private static async Task<IResult> GetMerchantReceiptAsync(Guid tenantId, Guid orderId,
        ICurrentTenant current, MarketDbContext db, IConfiguration config, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        if (Actor(http) == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty) return Results.NotFound();
        var pay = await db.Set<ManualOrderPayment>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrderId == OrderId.From(orderId), ct);
        return pay is null ? Results.NotFound() : await ReceiptFileAsync(tenantId, pay.Id, db, config, http, ct);
    }
    private static async Task<IResult> ReceiptFileAsync(Guid tenantId, Guid paymentId,
        MarketDbContext db, IConfiguration config, HttpContext http, CancellationToken ct)
    {
        var receipt = await LatestAsync(db, paymentId, ct);
        if (receipt is null) return Results.NotFound();
        var bytes = ManualReceiptCrypto.Unprotect(config, tenantId, receipt.OrderId.Value,
            receipt.Id, receipt.Ciphertext, receipt.Nonce, receipt.AuthTag);
        NoStore(http);
        http.Response.Headers["Content-Disposition"] = "attachment; filename=transfer-receipt" +
            (receipt.ContentType == "image/png" ? ".png" : ".jpg");
        return Results.File(bytes, receipt.ContentType);
    }
    private static async Task<IResult> ReviewAsync(Guid tenantId, Guid orderId, ReviewManualReceiptRequest request,
        ICurrentTenant current, MarketDbContext db, IStockHoldLedger holds, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty) return Invalid("رقم الطلب غير صحيح.");
        var reason = request.Reason?.Trim();
        if (!request.Approve && (string.IsNullOrWhiteSpace(reason) || reason.Length > 500 || reason.Any(char.IsControl)))
            return Invalid("أدخل سبب رفض واضحًا لا يتجاوز 500 حرف.");
        try
        {
            return await ExecutePaymentTransactionAsync(db, async () =>
            {
            var order = await LockOrderAsync(db, tenantId, orderId, ct);
            if (order is null) return Results.NotFound();
            var manual = await LockManualAsync(db, tenantId, orderId, ct);
            if (manual is null) return Results.NotFound();
            if (manual.Status != "PendingReview" || order.Status != OrderStatus.Pending)
                return Conflict("الطلب لم يعد بانتظار مراجعة الدفع.");
            var receipt = await LatestAsync(db, manual.Id, ct);
            if (receipt is null || receipt.ReviewStatus != "PendingReview")
                return Conflict("لا يوجد إيصال جديد بانتظار المراجعة.");
            var now = DateTimeOffset.UtcNow;
            if (request.Approve)
            {
                // A previous electronic payment attempt must never be auto-approved as a manual transfer.
                if (await db.Payments.AnyAsync(p => p.OrderId == order.Id, ct))
                    return Conflict("الطلب لديه سجل دفع سابق ويتطلب مراجعة مستقلة.");
                if (manual.Amount != order.TotalAmount || manual.Currency != order.Currency.Value)
                    return Conflict("قيمة الطلب أو عملته تغيّرت، لا يمكن اعتماد إيصال قديم.");
                // Physical stock changes once, atomically with payment approval.
                await holds.CaptureOrderAsync(order, now, actor, ct);
                var payment = Payment.Create(current.TenantId!.Value, order.Id, order.CustomerUserId,
                    Money.Create(order.TotalAmount, order.Currency), now, actor);
                payment.MarkSucceeded(now, actor);
                db.Payments.Add(payment);
                order.MarkPaid(now, actor);
                order.Confirm(now, actor);
                var checkoutCart = await db.Carts.Include("_items").SingleOrDefaultAsync(
                    c => c.Id == order.SourceCartId, ct);
                if (checkoutCart?.Status == CartStatus.Active)
                    checkoutCart.MarkConverted(now, actor);
                // Actual preparation requires an explicit action by the merchant.
                manual.Approve(now);
            }
            else
            {
                await holds.ReleaseOrderAsync(order.Id, ct);
                manual.Reject(now);
            }
            receipt.Review(request.Approve, reason, actor, now);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { orderId, status = manual.Status, orderStatus = order.Status.ToString() });
            }, ct);
        }
        catch (StockHoldExpiredException)
        {
            return Conflict("انتهت صلاحية حجز أحد المنتجات؛ لا تعتمد الدفع قبل تسوية المخزون والمبلغ يدويًا.");
        }
        catch (StockHoldUnavailableException)
        {
            return Conflict("المخزون غير كافٍ؛ لا تعتمد إيصال الدفع قبل تسوية الحالة مع العميل.");
        }
    }
}
