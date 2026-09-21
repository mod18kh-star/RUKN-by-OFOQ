using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
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
        var merchant = endpoints.MapGroup("/api/tenants/{tenantId:guid}/backoffice/manual-payments")
            .WithTags("Manual Payment Review")
            .RequireAuthorization(AuthorizationPolicies.TenantPaymentAdministration);
        merchant.MapGet("/", GetMerchantPaymentsAsync);
        merchant.MapGet("/{orderId:guid}/receipt", GetMerchantReceiptAsync);
        merchant.MapPost("/{orderId:guid}/review", ReviewAsync);
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

    private static async Task<IResult> GetMethodsAsync(Guid tenantId, ICurrentTenant current,
        MarketDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        if (Actor(http) == Guid.Empty) return Results.Unauthorized();
        var rows = await db.Set<MerchantManualPaymentAccount>().AsNoTracking()
            .Where(x => x.IsEnabled).OrderBy(x => x.DisplayName).ToArrayAsync(ct);
        return Results.Ok(rows.Select(x => new ManualMethodSummary(x.Id, x.Kind, x.DisplayName, x.MaskedReference)));
    }

    private static async Task<IResult> GetMineAsync(Guid tenantId, ICurrentTenant current,
        MarketDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        var items = await db.Set<ManualOrderPayment>().AsNoTracking()
            .Where(p => p.CustomerUserId == actor).OrderByDescending(p => p.CreatedAtUtc)
            .Take(100).ToArrayAsync(ct);
        var result = new List<object>(items.Length);
        foreach (var payment in items)
        {
            var receipt = await LatestAsync(db, payment.Id, ct);
            result.Add(new { orderId = payment.OrderId.Value, payment.Status, payment.Amount,
                payment.Currency, receipt?.RejectionReason, payment.CreatedAtUtc });
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
        MarketDbContext db, IConfiguration configuration, HttpContext http, CancellationToken ct)
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
                var receiptId = Guid.NewGuid();
                var encrypted = ManualReceiptCrypto.Protect(configuration, tenantId, orderId, receiptId, content);
                var receipt = ManualPaymentReceipt.Create(current.TenantId!.Value, payment.Id, order.Id,
                    actor, mime, encrypted.Ciphertext, encrypted.Nonce, encrypted.Tag,
                    reference.Length == 0 ? null : reference, DateTimeOffset.UtcNow, receiptId);
                db.Set<ManualPaymentReceipt>().Add(receipt);
                payment.Submit(DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { orderId, receiptId, status = "PendingReview" });
            }, ct);
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
        ICurrentTenant current, MarketDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, current)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        if (orderId == Guid.Empty) return Invalid("رقم الطلب غير صحيح.");
        var reason = request.Reason?.Trim();
        if (!request.Approve && (string.IsNullOrWhiteSpace(reason) || reason.Length > 500 || reason.Any(char.IsControl)))
            return Invalid("أدخل سبب رفض واضحًا لا يتجاوز 500 حرف.");
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
                var payment = Payment.Create(current.TenantId!.Value, order.Id, order.CustomerUserId,
                    Money.Create(order.TotalAmount, order.Currency), now, actor);
                payment.MarkSucceeded(now, actor);
                db.Payments.Add(payment);
                order.MarkPaid(now, actor);
                order.Confirm(now, actor);
                order.StartProcessing(now, actor);
                manual.Approve(now);
            }
            else manual.Reject(now);
            receipt.Review(request.Approve, reason, actor, now);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { orderId, status = manual.Status, orderStatus = order.Status.ToString() });
        }, ct);
    }
}
