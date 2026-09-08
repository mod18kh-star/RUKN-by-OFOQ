using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Configuration;

public sealed class TenantCommerceVertical :
    AggregateRoot<TenantCommerceVerticalId>,
    ITenantDataScoped,
    IAuditable
{
    private TenantCommerceVertical()
    {
    }

    private TenantCommerceVertical(
        TenantCommerceVerticalId id,
        TenantId tenantId,
        CommerceVerticalType verticalType,
        bool isPrimary,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        ValidateTenantId(
            tenantId);

        ValidateVerticalType(
            verticalType);

        TenantId =
            tenantId;

        VerticalType =
            verticalType;

        IsEnabled =
            true;

        IsPrimary =
            isPrimary;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public CommerceVerticalType VerticalType { get; private set; }

    public bool IsEnabled { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantCommerceVertical Create(
        TenantId tenantId,
        CommerceVerticalType verticalType,
        bool isPrimary,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantCommerceVertical(
            TenantCommerceVerticalId.New(),
            tenantId,
            verticalType,
            isPrimary,
            createdAtUtc,
            createdByUserId);
    }

    public void Enable(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (IsEnabled)
        {
            return;
        }

        IsEnabled =
            true;

        Touch(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Disable(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (IsPrimary)
        {
            throw new InvalidOperationException(
                "The primary commerce vertical cannot be disabled. Select another primary vertical first.");
        }

        IsEnabled =
            false;

        Touch(
            updatedAtUtc,
            updatedByUserId);
    }

    public void MakePrimary(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (!IsEnabled)
        {
            IsEnabled =
                true;
        }

        if (IsPrimary)
        {
            return;
        }

        IsPrimary =
            true;

        Touch(
            updatedAtUtc,
            updatedByUserId);
    }

    public void RemovePrimary(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (!IsPrimary)
        {
            return;
        }

        IsPrimary =
            false;

        Touch(
            updatedAtUtc,
            updatedByUserId);
    }

    private void Touch(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
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

    private static void ValidateVerticalType(
        CommerceVerticalType verticalType)
    {
        if (verticalType == CommerceVerticalType.Unknown ||
            !Enum.IsDefined(
                verticalType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(verticalType),
                "A supported commerce vertical is required.");
        }
    }
}