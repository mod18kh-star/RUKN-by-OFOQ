using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Customers;

public sealed class CustomerProfile :
    Entity<CustomerProfileId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxDisplayNameLength = 160;
    public const int MaxPhoneLength = 40;
    public const int MaxNotesLength = 2000;

    private CustomerProfile() { }

    private CustomerProfile(
        CustomerProfileId id,
        TenantId tenantId,
        UserId userId,
        string? displayName,
        string? phone,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (userId.IsEmpty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        TenantId = tenantId;
        UserId = userId;
        DisplayName = NormalizeOptional(displayName, MaxDisplayNameLength, "Display name");
        Phone = NormalizeOptional(phone, MaxPhoneLength, "Phone");
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }
    public UserId UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Phone { get; private set; }
    public string? MerchantNotes { get; private set; }
    public bool IsBlocked { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static CustomerProfile Create(
        TenantId tenantId,
        UserId userId,
        string? displayName,
        string? phone,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
        => new(CustomerProfileId.New(), tenantId, userId, displayName, phone, createdAtUtc, createdByUserId);

    public void UpdateSelf(
        string? displayName,
        string? phone,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        DisplayName = NormalizeOptional(displayName, MaxDisplayNameLength, "Display name");
        Phone = NormalizeOptional(phone, MaxPhoneLength, "Phone");
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    public void UpdateMerchantState(
        string? notes,
        bool isBlocked,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        MerchantNotes = NormalizeOptional(notes, MaxNotesLength, "Merchant notes");
        IsBlocked = isBlocked;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    private void MarkUpdated(DateTimeOffset at, Guid? by)
    {
        UpdatedAtUtc = at;
        UpdatedByUserId = by;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.");
        return normalized;
    }
}
