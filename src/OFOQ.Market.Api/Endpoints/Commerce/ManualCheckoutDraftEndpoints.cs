using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Commerce.Checkout;
using OFOQ.Market.Application.Commerce.StockReservations;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Commerce.Fulfillment;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Endpoints.Commerce;

/// <summary>
/// Manual-payment preview does not create an Order. Order, payment, receipt,
/// stock hold transfer, coupon redemption and cart conversion are committed
/// atomically only when the customer submits payment evidence.
/// Existing /checkout and /orders/{id}/receipt endpoints remain for legacy orders.
/// </summary>
public static class ManualCheckoutDraftEndpoints
{
    private const int MaxReceiptBytes = 1024 * 1024;
    private sealed record DraftLine(Guid ProductVariantId, int Quantity, decimal UnitPrice);
    private sealed class DraftConflictException : Exception
    {
        public DraftConflictException(string message) : base(message) { }
    }

    public static IEndpointRouteBuilder MapManualCheckoutDraftEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tenants/{tenantId:guid}/payments/manual-checkout/draft")
            .WithTags("Manual Checkout Draft").RequireAuthorization();
        group.MapGet("/accounts/{accountId:guid}", PreviewAccountAsync);
        group.MapGet("/accounts/{accountId:guid}/qr", PreviewQrAsync);
        group.MapPost("/submit", SubmitReceiptAsync);
        return endpoints;
    }

    private static Guid Actor(HttpContext http) =>
        Guid.TryParse(http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : Guid.Empty;

    private static bool InScope(Guid tenantId, ICurrentTenant current) =>
        tenantId != Guid.Empty && current.IsAvailable && current.TenantId.HasValue &&
        current.TenantId.Value.Value == tenantId;

    private static IResult Conflict(string message) => Results.Conflict(new { message });

    private static async Task<MerchantManualPaymentAccount?> AccountAsync(
        MarketDbContext db, Guid tenantId, Guid accountId, CancellationToken ct) =>
        await db.Set<MerchantManualPaymentAccount>().AsNoTracking().SingleOrDefaultAsync(
            a => a.Id == accountId && a.TenantId == TenantId.From(tenantId) && a.IsEnabled, ct);

    private static async Task<bool> HasActiveCartAsync(MarketDbContext db, Guid tenantId, Guid actor,
        CancellationToken ct)
    {
        var cart = await db.Carts.FromSqlInterpolated(
            $"SELECT * FROM commerce_carts WHERE tenant_id = {tenantId} AND customer_user_id = {actor} AND status = 0")
            .AsNoTracking().Include("_items").SingleOrDefaultAsync(ct);
        return cart?.Items.Count > 0;
    }

    private static async Task<IResult> PreviewAccountAsync(Guid tenantId, Guid accountId,
        ICurrentTenant current, MarketDbContext db, IPaymentProviderCredentialProtector protector,
        HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http);
        if (actor == Guid.Empty) return Results.Unauthorized();
        if (!await HasActiveCartAsync(db, tenantId, actor, ct)) return Conflict("السلة غير متاحة. ارجع إلى سلتك.");
        var account = await AccountAsync(db, tenantId, accountId, ct);
        if (account is null) return Results.NotFound();
        var fields = protector.Unprotect(current.TenantId!.Value,
            TenantPaymentProviderAccountId.From(account.Id), account.ProtectedDetails).Values;
        string? Get(string key) => fields.TryGetValue(key, out var value) ? value : null;
        return Results.Ok(new
        {
            accountId = account.Id, accountName = account.DisplayName, accountKind = account.Kind,
            bankName = Get("bankName"), accountHolder = Get("accountHolder"), iban = Get("iban"),
            accountNumber = Get("accountNumber"), walletProvider = Get("walletProvider"),
            walletNumber = Get("walletNumber"), transferLink = Get("transferLink"),
            hasQr = fields.ContainsKey("qr"), accountUpdatedAtUtc = account.UpdatedAtUtc
        });
    }

    private static async Task<IResult> PreviewQrAsync(Guid tenantId, Guid accountId,
        ICurrentTenant current, MarketDbContext db, IPaymentProviderCredentialProtector protector,
        HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http);
        if (actor == Guid.Empty) return Results.Unauthorized();
        if (!await HasActiveCartAsync(db, tenantId, actor, ct)) return Results.NotFound();
        var account = await AccountAsync(db, tenantId, accountId, ct);
        if (account is null) return Results.NotFound();
        var fields = protector.Unprotect(current.TenantId!.Value,
            TenantPaymentProviderAccountId.From(account.Id), account.ProtectedDetails).Values;
        if (!fields.TryGetValue("qr", out var encoded)) return Results.NotFound();
        byte[] bytes;
        try { bytes = Convert.FromBase64String(encoded); }
        catch (FormatException) { return Results.NotFound(); }
        if (bytes.Length is < 24 or > 40960) return Results.NotFound();
        var png = bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217;
        if (!png && !jpeg) return Results.NotFound();
        return Results.File(bytes, png ? "image/png" : "image/jpeg");
    }

    private static async Task<IResult> SubmitReceiptAsync(Guid tenantId, ICurrentTenant current,
        MarketDbContext db, CheckoutHandler checkout, IStockHoldLedger holds,
        IPaymentProviderCredentialProtector protector, IConfiguration configuration,
        HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http);
        if (actor == Guid.Empty) return Results.Unauthorized();
        if (!holds.Enabled) return Results.StatusCode(503); // Never mix legacy immediate stock deduction with this workflow.
        if (http.Request.ContentLength is > MaxReceiptBytes + 65536 || !http.Request.HasFormContentType)
            return Results.BadRequest(new { message = "صيغة طلب الدفع أو حجم الملف غير صالح." });
        var sizeLimit = http.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeLimit is { IsReadOnly: false }) sizeLimit.MaxRequestBodySize = MaxReceiptBytes + 65536;
        var form = await http.Request.ReadFormAsync(ct);
        if (!Guid.TryParse(form["accountId"].ToString(), out var accountId) || accountId == Guid.Empty ||
            !Guid.TryParse(form["shippingMethodId"].ToString(), out var shippingId) || shippingId == Guid.Empty ||
            !Guid.TryParse(form["cartId"].ToString(), out var cartId) || cartId == Guid.Empty ||
            !Guid.TryParse(form["idempotencyKey"].ToString(), out var requestKey) || requestKey == Guid.Empty ||
            !decimal.TryParse(form["expectedAmount"].ToString(), NumberStyles.Number, CultureInfo.InvariantCulture,
                out var expectedAmount) || expectedAmount <= 0 ||
            !DateTimeOffset.TryParse(form["accountUpdatedAtUtc"].ToString(), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var expectedAccountUpdatedAt))
            return Results.BadRequest(new { message = "بيانات المسودة غير مكتملة؛ ارجع إلى صفحة إتمام الطلب." });

        OFOQ.Market.Domain.Commerce.Customers.CustomerAddressId? customerAddressId = null;
        var customerAddressRaw = form["customerAddressId"].ToString().Trim();

        if (!string.IsNullOrEmpty(customerAddressRaw))
        {
            if (!Guid.TryParse(customerAddressRaw, out var customerAddressGuid) ||
                customerAddressGuid == Guid.Empty)
                return Results.BadRequest(new { message = "عنوان التوصيل غير صالح؛ ارجع إلى صفحة إتمام الطلب." });

            customerAddressId =
                OFOQ.Market.Domain.Commerce.Customers.CustomerAddressId.From(customerAddressGuid);
        }

        var expectedCurrency = form["expectedCurrency"].ToString().Trim().ToUpperInvariant();
        if (expectedCurrency.Length != 3 || !expectedCurrency.All(char.IsLetter))
            return Results.BadRequest(new { message = "عملة المسودة غير صالحة." });
        var phone = form["customerPhone"].ToString().Trim();
        if (phone.Length is < 7 or > 40 || phone.Count(char.IsDigit) < 7 ||
            phone.Any(c => !(char.IsDigit(c) || c is '+' or '-' or ' ' or '(' or ')')))
            return Results.BadRequest(new { message = "رقم الهاتف غير صالح." });
        var coupon = form["couponCode"].ToString().Trim();
        if (coupon.Length > 60) return Results.BadRequest(new { message = "رمز الكوبون غير صالح." });
        var reference = form["transferReference"].ToString().Trim();
        if (reference.Length > 100 || reference.Any(char.IsControl))
            return Results.BadRequest(new { message = "مرجع التحويل غير صالح." });
        var linesJson = form["expectedLines"].ToString();
        if (linesJson.Length is < 2 or > 16000)
            return Results.BadRequest(new { message = "بيانات مشتريات المسودة غير صالحة." });
        DraftLine[]? expectedLines;
        try { expectedLines = JsonSerializer.Deserialize<DraftLine[]>(linesJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch (JsonException) { return Results.BadRequest(new { message = "بيانات مشتريات المسودة غير صالحة." }); }
        if (expectedLines is null || expectedLines.Length is < 1 or > 100 ||
            expectedLines.Any(l => l.ProductVariantId == Guid.Empty || l.Quantity <= 0 || l.UnitPrice < 0) ||
            expectedLines.Select(l => l.ProductVariantId).Distinct().Count() != expectedLines.Length)
            return Results.BadRequest(new { message = "محتويات مسودة الدفع غير صالحة." });

        var file = form.Files.GetFile("receipt");
        if (form.Files.Count != 1 || file is null || file.Length is < 24 or > MaxReceiptBytes)
            return Results.BadRequest(new { message = "أرفق إيصال PNG أو JPEG لا يتجاوز 1 MB." });
        var content = new byte[(int)file.Length];
        await using (var stream = file.OpenReadStream())
        {
            var pos = 0;
            while (pos < content.Length)
            {
                var count = await stream.ReadAsync(content.AsMemory(pos), ct);
                if (count == 0) return Results.BadRequest(new { message = "تعذر قراءة صورة الإيصال." });
                pos += count;
            }
        }
        try
        {
            if (!ValidImage(content, out var mime))
                return Results.BadRequest(new { message = "يجب رفع إيصال PNG أو JPEG صالح." });
            var strategy = db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    // Replay is safe even if the first response was lost after COMMIT.
                    async Task<IResult?> ReplayAsync()
                    {
                        var old = await db.Orders.Include("_items").SingleOrDefaultAsync(o =>
                            EF.Property<Guid>(o, "_customerUserId") == actor &&
                            EF.Property<string?>(o, "CheckoutIdempotencyKey") == requestKey.ToString(), ct);
                        if (old is null) return null;
                        if (old.SourceCartId.Value != cartId) throw new DraftConflictException("مسودة الدفع تغيرت. ارجع إلى السلة.");
                        var oldPayment = await db.Set<ManualOrderPayment>().AsNoTracking()
                            .SingleOrDefaultAsync(p => p.OrderId == old.Id && p.CustomerUserId == actor, ct);
                        if (oldPayment is null || oldPayment.AccountId != accountId ||
                            oldPayment.Status is not ("PendingReview" or "Approved" or "Rejected"))
                            throw new DraftConflictException("تعذر التحقق من نتيجة الطلب السابق؛ افتح صفحة طلباتك.");
                        return Results.Ok(new { orderId = old.Id.Value, status = oldPayment.Status, idempotentReplay = true });
                    }
                    var replay = await ReplayAsync();
                    if (replay is not null) { await tx.CommitAsync(ct); return replay; }

                    var cart = await db.Carts.FromSqlInterpolated(
                        $"SELECT * FROM commerce_carts WHERE tenant_id = {tenantId} AND customer_user_id = {actor} AND status = 0 FOR UPDATE")
                        .SingleOrDefaultAsync(ct);
                    if (cart is null)
                    {
                        replay = await ReplayAsync(); // Waited for concurrent submission on the same cart.
                        if (replay is not null) { await tx.CommitAsync(ct); return replay; }
                        throw new DraftConflictException("السلة لم تعد متاحة. افتح سلة التسوق لتحديث مشترياتك.");
                    }
                    await db.Entry(cart).Collection("_items").LoadAsync(ct);
                    if (cart.Id.Value != cartId || cart.Items.Count != expectedLines.Length ||
                        cart.Items.Any(item => !expectedLines.Any(line =>
                            line.ProductVariantId == item.ProductVariantId.Value &&
                            line.Quantity == item.Quantity && line.UnitPrice == item.UnitPrice.Amount)))
                        throw new DraftConflictException("محتويات السلة تغيرت منذ عرض بيانات التحويل. ارجع إلى إتمام الطلب لتحديث المبلغ. إذا حولت المال بالفعل، تواصل مع المتجر.");

                    var account = await AccountAsync(db, tenantId, accountId, ct);
                    if (account is null || account.UpdatedAtUtc != expectedAccountUpdatedAt)
                        throw new DraftConflictException("تغيرت بيانات الحساب البنكي أو توقّفت وسيلة الدفع. لا تُحوّل أي مبلغ جديد وتواصل مع المتجر إذا حولت بالفعل.");

                    var now = DateTimeOffset.UtcNow;

                    /*
                     * LEGACY_SOURCE_CART_ROTATION_V1
                     *
                     * Older deferred-checkout builds could leave a Pending Order
                     * attached to the still-active Cart before any receipt existed.
                     *
                     * The current manual-payment flow must never mutate or adopt
                     * that historical snapshot.
                     *
                     * If and only if the old Order is payment-free and uses the
                     * deferred stock ledger:
                     *
                     * 1. cancel the old historical Order,
                     * 2. release its stale Order hold,
                     * 3. release the current Cart holds,
                     * 4. close the old source Cart,
                     * 5. clone the CURRENT cart into a fresh active Cart,
                     * 6. let CheckoutHandler create the real immutable Order.
                     *
                     * Everything happens inside this receipt transaction. Any
                     * later pricing, inventory or receipt failure rolls all of
                     * these compatibility changes back atomically.
                     */
                    var legacyOrder = await db.Orders.FromSqlInterpolated(
                        $"SELECT * FROM commerce_orders WHERE tenant_id = {tenantId} AND source_cart_id = {cart.Id.Value} FOR UPDATE")
                        .SingleOrDefaultAsync(ct);

                    if (legacyOrder is not null)
                    {
                        await db.Entry(legacyOrder)
                            .Collection("_items")
                            .LoadAsync(ct);

                        await db.Entry(legacyOrder)
                            .Collection("_inventoryMovements")
                            .LoadAsync(ct);

                        if (legacyOrder.Status != OrderStatus.Pending ||
                            legacyOrder.CustomerUserId.Value != actor ||
                            legacyOrder.SourceCartId != cart.Id)
                            throw new DraftConflictException(
                                "هذه السلة مرتبطة بطلب سابق لا يمكن استبداله بأمان؛ راجع طلباتك.");

                        /*
                         * Fail closed for old immediate-stock-deduction Orders.
                         *
                         * A valid deferred marker plus zero checkout deductions
                         * proves that replacing this pre-receipt legacy snapshot
                         * cannot double-restore or double-deduct physical stock.
                         */
                        if (!await holds.IsDeferredOrderAsync(legacyOrder.Id, ct) ||
                            legacyOrder.InventoryMovements.Any(movement =>
                                movement.Type == InventoryMovementType.CheckoutDeduction &&
                                movement.QuantityDelta < 0))
                            throw new DraftConflictException(
                                "هذا الطلب القديم استخدم مخزونًا فعليًا ولا يمكن استبداله تلقائيًا؛ تواصل مع المتجر.");

                        var legacyManual =
                            await db.Set<ManualOrderPayment>()
                                .FromSqlInterpolated(
                                    $"SELECT * FROM commerce_manual_order_payments WHERE tenant_id = {tenantId} AND order_id = {legacyOrder.Id.Value} FOR UPDATE")
                                .SingleOrDefaultAsync(ct);

                        var hasReceipt =
                            await db.Set<ManualPaymentReceipt>()
                                .AsNoTracking()
                                .AnyAsync(
                                    receipt =>
                                        receipt.OrderId == legacyOrder.Id,
                                    ct);

                        var hasProviderPayment =
                            await db.Payments
                                .AsNoTracking()
                                .AnyAsync(
                                    payment =>
                                        payment.OrderId == legacyOrder.Id,
                                    ct);

                        if (hasReceipt ||
                            hasProviderPayment ||
                            (legacyManual is not null &&
                             (legacyManual.CustomerUserId != actor ||
                              legacyManual.Status != "AwaitingReceipt")))
                            throw new DraftConflictException(
                                "هذه السلة مرتبطة بطلب سبق بدء دفعه أو إرسال إثبات له؛ راجع طلباتك.");

                        /*
                         * Preserve exactly what the customer has in the CURRENT
                         * cart. CheckoutHandler will still re-price all products
                         * from the authoritative catalog immediately afterward.
                         */
                        var replacementCart =
                            Cart.Create(
                                current.TenantId!.Value,
                                UserId.From(actor),
                                now,
                                actor);

                        foreach (var item in cart.Items)
                        {
                            replacementCart.AddItem(
                                item.ProductId,
                                item.ProductVariantId,
                                item.UnitPrice,
                                item.Quantity,
                                now,
                                actor);
                        }

                        legacyOrder.Cancel(
                            "تم إغلاق مسودة طلب قديمة غير مدفوعة واستبدالها بالسلة الحالية عند إرسال إثبات الدفع.",
                            now,
                            actor);

                        if (legacyManual is not null)
                            legacyManual.CancelBeforeReceipt(now);

                        /*
                         * Release both stale ownership paths before the fresh
                         * CheckoutHandler reservation runs.
                         */
                        await holds.ReleaseOrderAsync(
                            legacyOrder.Id,
                            ct);

                        await holds.ReleaseCartAsync(
                            cart.Id,
                            ct);

                        /*
                         * The old unpaid compatibility Order must not consume
                         * coupon quota after it is cancelled.
                         */
                        var staleRedemptions =
                            await db.CouponRedemptions
                                .Where(
                                    redemption =>
                                        redemption.OrderId == legacyOrder.Id)
                                .ToArrayAsync(ct);

                        if (staleRedemptions.Length > 0)
                            db.CouponRedemptions.RemoveRange(
                                staleRedemptions);

                        /*
                         * The old Cart remains as historical source data for
                         * the cancelled Order. It must no longer be Active,
                         * otherwise the one-active-cart invariant would block
                         * the replacement cart.
                         */
                        cart.MarkConverted(
                            now,
                            actor);

                        await db.SaveChangesAsync(ct);

                        db.Carts.Add(
                            replacementCart);

                        await db.SaveChangesAsync(ct);

                        cart =
                            replacementCart;
                    }

                    /*
                     * Normal V1 path.
                     *
                     * CheckoutHandler now operates on either:
                     * - the original current Cart when no legacy Order exists, or
                     * - the fresh replacement Cart created above.
                     *
                     * It re-checks authoritative prices, product/variant status,
                     * shipping, coupon limits and stock under this transaction.
                     */
                    var checkoutResult =
                        await checkout.HandleWithinExistingTransactionAsync(
                            new CheckoutCommand(
                                UserId.From(actor),
                                requestKey.ToString(),
                                customerAddressId,
                                ShippingMethodId.From(shippingId),
                                string.IsNullOrWhiteSpace(coupon)
                                    ? null
                                    : coupon),
                            ct);

                    /*
                     * Never weaken draft validation. A changed authoritative
                     * price, currency, shipping amount or coupon still blocks
                     * receipt submission and rolls back the entire operation.
                     */
                    if (checkoutResult.SourceCartId != cart.Id ||
                        checkoutResult.Currency != expectedCurrency ||
                        checkoutResult.TotalAmount != expectedAmount)
                        throw new DraftConflictException(
                            "قيمة الطلب أو عملته تغيرت. حدّث السلة قبل تحويل أي مبلغ، وتواصل مع المتجر إذا تم التحويل بالفعل.");

                    var order =
                        await db.Orders
                            .Include("_items")
                            .SingleAsync(
                                value =>
                                    value.Id == checkoutResult.OrderId,
                                ct);

                    if (order.Items.Count != expectedLines.Length ||
                        order.Items.Any(item =>
                            !expectedLines.Any(line =>
                                line.ProductVariantId ==
                                    item.ProductVariantId.Value &&
                                line.Quantity ==
                                    item.Quantity &&
                                line.UnitPrice ==
                                    item.UnitPrice.Amount)))
                        throw new DraftConflictException(
                            "محتويات الطلب لا تطابق السلة الحالية. ارجع إلى السلة وأعد المحاولة.");
                    await holds.HoldOrderForReviewAsync(order, now, ct);
                    var details = protector.Unprotect(current.TenantId!.Value,
                        TenantPaymentProviderAccountId.From(account.Id), account.ProtectedDetails);
                    var manualPaymentId = Guid.NewGuid();
                    var snapshot = protector.Protect(current.TenantId.Value,
                        TenantPaymentProviderAccountId.From(manualPaymentId),
                        PaymentProviderCredentialPayload.Create(details.Values));
                    var manual = ManualOrderPayment.Create(current.TenantId.Value, order.Id, actor,
                        manualPaymentId, account.Id, account.DisplayName, account.Kind, snapshot,
                        order.TotalAmount, order.Currency.Value, phone, now);
                    manual.Submit(now);
                    db.Set<ManualOrderPayment>().Add(manual);
                    var receiptId = Guid.NewGuid();
                    var encrypted = ManualReceiptCrypto.Protect(configuration, tenantId, order.Id.Value, receiptId, content);
                    db.Set<ManualPaymentReceipt>().Add(ManualPaymentReceipt.Create(current.TenantId.Value,
                        manual.Id, order.Id, actor, mime, encrypted.Ciphertext, encrypted.Nonce,
                        encrypted.Tag, string.IsNullOrWhiteSpace(reference) ? null : reference, now, receiptId));
                    cart.MarkConverted(now, actor);
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return Results.Ok(new { orderId = order.Id.Value, receiptId, status = "PendingReview", idempotentReplay = false });
                }
                catch
                {
                    await tx.RollbackAsync(CancellationToken.None);
                    throw;
                }
            });
        }
        catch (DraftConflictException ex) { return Conflict(ex.Message); }
        catch (CheckoutInsufficientStockException) { return Conflict("الكمية المطلوبة لم تعد متاحة. إذا حولت المال بالفعل تواصل مع المتجر."); }
        catch (StockHoldUnavailableException) { return Conflict("تعذر حجز الكمية المطلوبة. إذا حولت المال بالفعل تواصل مع المتجر."); }
        catch (CheckoutPricingException) { return Conflict("السعر أو الشحن أو الكوبون تغير؛ أعد مراجعة المبلغ قبل الدفع."); }
        catch (CheckoutIdempotencyConflictException) { return Conflict("هذه المسودة مرتبطة بعملية شراء سابقة؛ راجع طلباتك."); }
        finally { CryptographicOperations.ZeroMemory(content); }
    }

    private static bool ValidImage(ReadOnlySpan<byte> bytes, out string mime)
    {
        mime = "";
        if (bytes.Length is < 24 or > MaxReceiptBytes) return false;
        if (bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
            bytes.Slice(12, 4).SequenceEqual("IHDR"u8))
        {
            var w = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4));
            var h = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4));
            if (w is < 1 or > 8192 || h is < 1 or > 8192) return false;
            mime = "image/png"; return true;
        }
        if (bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217)
        {
            mime = "image/jpeg"; return true;
        }
        return false;
    }
}
