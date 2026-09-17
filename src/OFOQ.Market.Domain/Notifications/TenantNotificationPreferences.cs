using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Notifications;

public sealed class TenantNotificationPreferences :
    Entity<TenantNotificationPreferencesId>,
    ITenantDataScoped,
    IAuditable
{
    private TenantNotificationPreferences()
    {
    }

    private TenantNotificationPreferences(
        TenantNotificationPreferencesId id,
        TenantId tenantId,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));

        TenantId = tenantId;
        NewOrderEmailEnabled = true;
        LowStockEmailEnabled = true;
        ReviewEmailEnabled = true;
        PlatformRequestEmailEnabled = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public bool NewOrderEmailEnabled { get; private set; }

    public bool LowStockEmailEnabled { get; private set; }

    public bool ReviewEmailEnabled { get; private set; }

    public bool PlatformRequestEmailEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantNotificationPreferences Create(
        TenantId tenantId,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
        => new(
            TenantNotificationPreferencesId.New(),
            tenantId,
            createdAtUtc,
            createdByUserId);

    public void Update(
        bool newOrderEmailEnabled,
        bool lowStockEmailEnabled,
        bool reviewEmailEnabled,
        bool platformRequestEmailEnabled,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        NewOrderEmailEnabled = newOrderEmailEnabled;
        LowStockEmailEnabled = lowStockEmailEnabled;
        ReviewEmailEnabled = reviewEmailEnabled;
        PlatformRequestEmailEnabled = platformRequestEmailEnabled;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }
}
