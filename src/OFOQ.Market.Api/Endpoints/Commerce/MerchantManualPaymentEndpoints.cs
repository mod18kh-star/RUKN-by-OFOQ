using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Endpoints.Commerce;

// Stage 2B: merchant-side configuration ONLY. Not a payment processor, a customer
// payment method, a confirmed receipt, or a RUKN-held merchant wallet.
public static class MerchantManualPaymentEndpoints
{
    private const int MaxQrBytes = 40 * 1024;
    private const int MaxAccounts = 30;

    public sealed record SaveManualAccountRequest(
        string Kind, string DisplayName, string? BankName, string? AccountHolder,
        string? Iban, string? AccountNumber, string? WalletProvider,
        string? WalletNumber, string? TransferLink, string? QrBase64, bool RemoveQr);
    public sealed record SetManualAccountStateRequest(bool Enabled);
    public sealed record ManualAccountSummary(Guid Id, string Kind, string DisplayName,
        string MaskedReference, bool HasQr, bool IsEnabled, DateTimeOffset UpdatedAtUtc);
    public sealed record ManualAccountDetails(Guid Id, string Kind, string DisplayName,
        string? BankName, string? AccountHolder, string? Iban, string? AccountNumber,
        string? WalletProvider, string? WalletNumber, string? TransferLink, bool HasQr, bool IsEnabled);

    public static IEndpointRouteBuilder MapMerchantManualPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tenants/{tenantId:guid}/payments/manual-accounts")
            .WithTags("Merchant Manual Payment Accounts")
            .RequireAuthorization(AuthorizationPolicies.TenantPaymentAdministration);
        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetOneAsync);
        group.MapGet("/{id:guid}/qr", GetQrAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPut("/{id:guid}/state", SetStateAsync);
        return endpoints;
    }

    private static bool InScope(Guid routeTenantId, ICurrentTenant currentTenant) =>
        routeTenantId != Guid.Empty && currentTenant.IsAvailable &&
        currentTenant.TenantId.HasValue && currentTenant.TenantId.Value.Value == routeTenantId;

    private static Guid Actor(HttpContext context) =>
        Guid.TryParse(context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var actor)
            ? actor : Guid.Empty;

    private static ManualAccountSummary Summary(MerchantManualPaymentAccount x) =>
        new(x.Id, x.Kind, x.DisplayName, x.MaskedReference, x.HasQr, x.IsEnabled, x.UpdatedAtUtc);

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > maxLength || trimmed.Any(char.IsControl))
            throw new ArgumentException($"{field} is missing or invalid.");
        return trimmed;
    }

    private static string Optional(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length > maxLength || trimmed.Any(char.IsControl))
            throw new ArgumentException($"{field} is invalid.");
        return trimmed;
    }

    private static string ValidateLink(string? value, IConfiguration configuration)
    {
        var link = Optional(value, "transferLink", 500);
        if (link.Length == 0) return string.Empty;
        if (!Uri.TryCreate(link, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps ||
            parsed.Port != 443 || parsed.UserInfo.Length > 0 || parsed.Fragment.Length > 0 ||
            parsed.HostNameType != UriHostNameType.Dns || parsed.IdnHost.Length == 0)
            throw new ArgumentException("transferLink must be an HTTPS URL without credentials, fragment or custom port.");
        var hosts = configuration.GetSection("Payments:MerchantLinkAllowedHosts").GetChildren()
            .Select(x => x.Value?.Trim().TrimEnd('.')).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (!hosts.Any(host => string.Equals(host, parsed.IdnHost.TrimEnd('.'), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("transferLink host is not approved for this installation.");
        // Stored for future approved integration; it is NOT currently exposed to customers.
        return parsed.AbsoluteUri;
    }

    private static bool ValidIban(string iban)
    {
        if (iban.Length is < 15 or > 34 ||
            !System.Text.RegularExpressions.Regex.IsMatch(iban, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$"))
            return false;
        var rearranged = iban[4..] + iban[..4];
        var remainder = 0;
        foreach (var symbol in rearranged)
        {
            if (symbol is >= '0' and <= '9')
                remainder = (remainder * 10 + (symbol - '0')) % 97;
            else if (symbol is >= 'A' and <= 'Z')
            {
                var numeric = symbol - 'A' + 10;
                remainder = (remainder * 100 + numeric) % 97;
            }
            else return false;
        }
        return remainder == 1;
    }

    private static string Mask(string source)
    {
        var compact = new string(source.Where(char.IsLetterOrDigit).ToArray());
        return compact.Length <= 4 ? "••••" : "•••• " + compact[^4..];
    }

    private static byte[] ValidateQr(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded) || encoded.Length > 56000 || encoded.Contains(','))
            throw new ArgumentException("Invalid QR image payload.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(encoded); }
        catch (FormatException) { throw new ArgumentException("QR image must be Base64 encoded."); }
        if (bytes.Length < 24 || bytes.Length > MaxQrBytes) throw new ArgumentException("QR image size is invalid.");
        var png = bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff && bytes[^2] == 0xff && bytes[^1] == 0xd9;
        if (!png && !jpeg) throw new ArgumentException("Only PNG and JPEG QR images are supported.");
        if (png)
        {
            if (!bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8) ||
                System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)) is 0 or > 2048 ||
                System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)) is 0 or > 2048)
                throw new ArgumentException("PNG image dimensions are invalid.");
        }
        // Header/size checks are not a malware scan. Public QR serving remains disabled.
        return bytes;
    }

    private static Dictionary<string, string> BuildFields(SaveManualAccountRequest request,
        IConfiguration configuration, Dictionary<string, string>? old = null)
    {
        var kind = Required(request.Kind, "kind", 20).ToLowerInvariant();
        if (kind is not ("bank" or "wallet")) throw new ArgumentException("Unsupported manual payment account kind.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (kind == "bank")
        {
            result["bankName"] = Required(request.BankName, "bankName", 120);
            result["accountHolder"] = Required(request.AccountHolder, "accountHolder", 120);
            // Not every country uses IBAN, so permit a domestic account/reference instead.
            var iban = Optional(request.Iban, "iban", 34).Replace(" ", "").ToUpperInvariant();
            if (iban.Length > 0 && !ValidIban(iban))
                throw new ArgumentException("IBAN format is invalid.");
            var accountNumber = Optional(request.AccountNumber, "accountNumber", 80);
            if (iban.Length == 0 && accountNumber.Length == 0)
                throw new ArgumentException("IBAN or account number is required.");
            if (iban.Length > 0) result["iban"] = iban;
            if (accountNumber.Length > 0) result["accountNumber"] = accountNumber;
        }
        else
        {
            result["walletProvider"] = Required(request.WalletProvider, "walletProvider", 120);
            result["accountHolder"] = Required(request.AccountHolder, "accountHolder", 120);
            result["walletNumber"] = Required(request.WalletNumber, "walletNumber", 80);
        }
        var url = ValidateLink(request.TransferLink, configuration);
        if (url.Length > 0) result["transferLink"] = url;
        if (!string.IsNullOrWhiteSpace(request.QrBase64))
            result["qr"] = Convert.ToBase64String(ValidateQr(request.QrBase64));
        else if (!request.RemoveQr && old is not null && old.TryGetValue("qr", out var oldQr))
            result["qr"] = oldQr;
        return result;
    }

    private static Dictionary<string, string> Unprotect(MerchantManualPaymentAccount account,
        IPaymentProviderCredentialProtector protector)
    {
        var decrypted = protector.Unprotect(account.TenantId,
            TenantPaymentProviderAccountId.From(account.Id), account.ProtectedDetails);
        return decrypted.Values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static string Protected(Guid id, TenantId tenantId,
        Dictionary<string, string> data, IPaymentProviderCredentialProtector protector) =>
        protector.Protect(tenantId, TenantPaymentProviderAccountId.From(id),
            PaymentProviderCredentialPayload.Create(data));

    private static string? Field(Dictionary<string, string> fields, string name) =>
        fields.TryGetValue(name, out var value) ? value : null;

    private static async Task<IResult> GetAllAsync(Guid tenantId, ICurrentTenant tenant,
        MarketDbContext db, CancellationToken ct)
    {
        if (!InScope(tenantId, tenant)) return Results.Forbid();
        var accounts = await db.Set<MerchantManualPaymentAccount>().AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
        return Results.Ok(accounts.Select(Summary).ToArray());
    }

    private static async Task<IResult> GetOneAsync(Guid tenantId, Guid id, ICurrentTenant tenant,
        MarketDbContext db, IPaymentProviderCredentialProtector protector, CancellationToken ct)
    {
        if (!InScope(tenantId, tenant)) return Results.Forbid();
        var account = await db.Set<MerchantManualPaymentAccount>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (account is null) return Results.NotFound();
        var data = Unprotect(account, protector);
        return Results.Ok(new ManualAccountDetails(account.Id, account.Kind, account.DisplayName,
            Field(data, "bankName"), Field(data, "accountHolder"), Field(data, "iban"),
            Field(data, "accountNumber"), Field(data, "walletProvider"), Field(data, "walletNumber"),
            Field(data, "transferLink"), account.HasQr, account.IsEnabled));
    }

    private static async Task<IResult> GetQrAsync(Guid tenantId, Guid id, ICurrentTenant tenant,
        MarketDbContext db, IPaymentProviderCredentialProtector protector, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, tenant)) return Results.Forbid();
        var account = await db.Set<MerchantManualPaymentAccount>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (account is null || !account.HasQr) return Results.NotFound();
        var data = Unprotect(account, protector);
        if (!data.TryGetValue("qr", out var qr)) return Results.NotFound();
        var bytes = ValidateQr(qr);
        http.Response.Headers["Cache-Control"] = "private, no-store, max-age=0";
        http.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.File(bytes, bytes[0] == 137 ? "image/png" : "image/jpeg");
    }

    private static async Task<IResult> CreateAsync(Guid tenantId, SaveManualAccountRequest request,
        ICurrentTenant tenant, MarketDbContext db, IPaymentProviderCredentialProtector protector,
        IConfiguration configuration, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, tenant)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        try
        {
            if (await db.Set<MerchantManualPaymentAccount>().CountAsync(ct) >= MaxAccounts)
                return Results.Conflict(new { message = "Maximum configured accounts reached." });
            var display = Required(request.DisplayName, "displayName", 120);
            var data = BuildFields(request, configuration);
            var id = Guid.NewGuid();
            var reference = request.Kind.Trim().Equals("bank", StringComparison.OrdinalIgnoreCase)
                ? Field(data, "iban") ?? Field(data, "accountNumber")! : Field(data, "walletNumber")!;
            var protectedPayload = Protected(id, tenant.TenantId!.Value, data, protector);
            var account = MerchantManualPaymentAccount.Create(tenant.TenantId.Value, id,
                request.Kind.Trim().ToLowerInvariant(), display, Mask(reference), protectedPayload,
                data.ContainsKey("qr"), actor, DateTimeOffset.UtcNow);
            db.Set<MerchantManualPaymentAccount>().Add(account);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/tenants/{tenantId}/payments/manual-accounts/{id}", Summary(account));
        }
        catch (ArgumentException) { return Results.BadRequest(new { message = "بيانات وسيلة الدفع غير مكتملة أو غير صالحة." }); }
    }

    private static async Task<IResult> UpdateAsync(Guid tenantId, Guid id, SaveManualAccountRequest request,
        ICurrentTenant tenant, MarketDbContext db, IPaymentProviderCredentialProtector protector,
        IConfiguration configuration, HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, tenant)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        var account = await db.Set<MerchantManualPaymentAccount>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (account is null) return Results.NotFound();
        if (!string.Equals(account.Kind, request.Kind?.Trim(), StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { message = "Cannot change payment method type after creation." });
        try
        {
            var data = BuildFields(request, configuration, Unprotect(account, protector));
            var reference = account.Kind == "bank" ? Field(data, "iban") ?? Field(data, "accountNumber")! : Field(data, "walletNumber")!;
            account.Update(Required(request.DisplayName, "displayName", 120), Mask(reference),
                Protected(id, account.TenantId, data, protector), data.ContainsKey("qr"),
                actor, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(Summary(account));
        }
        catch (ArgumentException) { return Results.BadRequest(new { message = "بيانات وسيلة الدفع غير مكتملة أو غير صالحة." }); }
    }

    private static async Task<IResult> SetStateAsync(Guid tenantId, Guid id, SetManualAccountStateRequest request,
        ICurrentTenant tenant, MarketDbContext db, IPaymentProviderCredentialProtector protector,
        HttpContext http, CancellationToken ct)
    {
        if (!InScope(tenantId, tenant)) return Results.Forbid();
        var actor = Actor(http); if (actor == Guid.Empty) return Results.Unauthorized();
        var account = await db.Set<MerchantManualPaymentAccount>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (account is null) return Results.NotFound();
        if (request.Enabled)
        {
            var fields = Unprotect(account, protector);
            if (!fields.ContainsKey("accountHolder") ||
                (account.Kind == "bank" && (!fields.ContainsKey("bankName") ||
                    (!fields.ContainsKey("iban") && !fields.ContainsKey("accountNumber")))) ||
                (account.Kind == "wallet" && (!fields.ContainsKey("walletProvider") || !fields.ContainsKey("walletNumber"))))
                return Results.Conflict(new { message = "Incomplete account details." });
        }
        account.SetEnabled(request.Enabled, actor, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        // Enabled means merchant-admin configured only; Stage 2B intentionally does NOT expose
        // methods to customers and does NOT mark any order/payment as paid.
        return Results.Ok(Summary(account));
    }
}
