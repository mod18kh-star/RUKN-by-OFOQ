using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Configuration;

public sealed class TenantCommerceCapabilityOverride :
    AggregateRoot<TenantCommerceCapabilityOverrideId>,
    ITenantDataScoped,
    IAuditable
{
    private TenantCommerceCapabilityOverride()
    {
    }

    private TenantCommerceCapabilityOverride(
        TenantCommerceCapabilityOverrideId id,
        TenantId tenantId,
        CommerceCapabilityType capabilityType,
        bool isEnabled,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        ValidateTenantId(
            tenantId);

        ValidateCapabilityType(
            capabilityType);

        TenantId =
            tenantId;

        CapabilityType =
            capabilityType;

        IsEnabled =
            isEnabled;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public CommerceCapabilityType CapabilityType { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantCommerceCapabilityOverride Create(
        TenantId tenantId,
        CommerceCapabilityType capabilityType,
        bool isEnabled,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantCommerceCapabilityOverride(
            TenantCommerceCapabilityOverrideId.New(),
            tenantId,
            capabilityType,
            isEnabled,
            createdAtUtc,
            createdByUserId);
    }

    public void SetEnabled(
        bool isEnabled,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (IsEnabled == isEnabled)
        {
            return;
        }

        IsEnabled =
            isEnabled;

        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private static void ValidateTenantId(
        TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }
    }

    private static void ValidateCapabilityType(
        CommerceCapabilityType capabilityType)
    {
        if (capabilityType == CommerceCapabilityType.Unknown ||
            !Enum.IsDefined(
                capabilityType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(capabilityType),
                "A supported commerce capability is required.");
        }
    }
}