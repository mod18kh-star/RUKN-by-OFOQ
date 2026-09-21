using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

// This is a merchant-owned configuration record, NOT a confirmed payment or a wallet balance.
public sealed class MerchantManualPaymentAccount : ITenantDataScoped
{
    private MerchantManualPaymentAccount() { }

    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string MaskedReference { get; private set; } = string.Empty;
    public string ProtectedDetails { get; private set; } = string.Empty;
    public bool HasQr { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedByUserId { get; private set; }

    public static MerchantManualPaymentAccount Create(
        TenantId tenantId, Guid id, string kind, string displayName,
        string maskedReference, string protectedDetails, bool hasQr,
        Guid actorUserId, DateTimeOffset now)
    {
        if (tenantId.IsEmpty || id == Guid.Empty || actorUserId == Guid.Empty)
            throw new ArgumentException("Valid tenant, account and actor identifiers are required.");
        return new MerchantManualPaymentAccount
        {
            Id = id, TenantId = tenantId, Kind = kind,
            DisplayName = displayName, MaskedReference = maskedReference,
            ProtectedDetails = protectedDetails, HasQr = hasQr, IsEnabled = false,
            CreatedAtUtc = now, UpdatedAtUtc = now,
            CreatedByUserId = actorUserId, UpdatedByUserId = actorUserId
        };
    }

    public void Update(string displayName, string maskedReference, string protectedDetails,
        bool hasQr, Guid actorUserId, DateTimeOffset now)
    {
        DisplayName = displayName;
        MaskedReference = maskedReference;
        ProtectedDetails = protectedDetails;
        HasQr = hasQr;
        IsEnabled = false; // material account changes always require an explicit re-enable
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = now;
    }

    public void SetEnabled(bool enabled, Guid actorUserId, DateTimeOffset now)
    {
        IsEnabled = enabled;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = now;
    }
}
