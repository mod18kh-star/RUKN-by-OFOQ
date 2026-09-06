using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

public sealed class TenantPaymentCapability :
    AggregateRoot<TenantPaymentCapabilityId>,
    ITenantDataScoped,
    IAuditable
{
    private TenantPaymentCapability()
    {
    }

    private TenantPaymentCapability(
        TenantPaymentCapabilityId id,
        TenantId tenantId,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        TenantId = tenantId;
        ElectronicPaymentsStatus = ElectronicPaymentStatus.Enabled;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public ElectronicPaymentStatus ElectronicPaymentsStatus { get; private set; }

    public string? SuspensionReason { get; private set; }

    public DateTimeOffset? SuspendedAtUtc { get; private set; }

    public Guid? SuspendedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool ElectronicPaymentsAllowed =>
        ElectronicPaymentsStatus == ElectronicPaymentStatus.Enabled;

    public static TenantPaymentCapability Create(
        TenantId tenantId,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantPaymentCapability(
            TenantPaymentCapabilityId.New(),
            tenantId,
            createdAtUtc,
            createdByUserId);
    }

    public void SuspendElectronicPayments(
        string reason,
        DateTimeOffset suspendedAtUtc,
        Guid suspendedByUserId)
    {
        if (suspendedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Suspending user ID cannot be empty.",
                nameof(suspendedByUserId));
        }

        var normalizedReason = NormalizeReason(reason);

        if (ElectronicPaymentsStatus == ElectronicPaymentStatus.Suspended)
        {
            SuspensionReason = normalizedReason;
            SuspendedAtUtc = suspendedAtUtc;
            SuspendedByUserId = suspendedByUserId;
            MarkUpdated(suspendedAtUtc, suspendedByUserId);
            return;
        }

        ElectronicPaymentsStatus = ElectronicPaymentStatus.Suspended;
        SuspensionReason = normalizedReason;
        SuspendedAtUtc = suspendedAtUtc;
        SuspendedByUserId = suspendedByUserId;
        MarkUpdated(suspendedAtUtc, suspendedByUserId);
    }

    public void ResumeElectronicPayments(
        DateTimeOffset resumedAtUtc,
        Guid resumedByUserId)
    {
        if (resumedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Resuming user ID cannot be empty.",
                nameof(resumedByUserId));
        }

        if (ElectronicPaymentsStatus == ElectronicPaymentStatus.Enabled)
        {
            return;
        }

        ElectronicPaymentsStatus = ElectronicPaymentStatus.Enabled;
        SuspensionReason = null;
        SuspendedAtUtc = null;
        SuspendedByUserId = null;
        MarkUpdated(resumedAtUtc, resumedByUserId);
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static string NormalizeReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Suspension reason is required.",
                nameof(value));
        }

        var normalized = value.Trim();

        if (normalized.Length > 500)
        {
            throw new ArgumentException(
                "Suspension reason cannot exceed 500 characters.",
                nameof(value));
        }

        return normalized;
    }
}
