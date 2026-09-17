using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Platform;

public sealed class StoreSubscription :
    Entity<StoreSubscriptionId>,
    IAuditable
{
    private StoreSubscription()
    {
    }

    private StoreSubscription(
        StoreSubscriptionId id,
        TenantId tenantId,
        string planCode,
        StoreBillingCycle billingCycle,
        DateTimeOffset startedAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId = tenantId;
        PlanCode = NormalizePlanCode(planCode);
        BillingCycle = billingCycle;
        Status = StoreSubscriptionStatus.Active;
        StartedAtUtc = startedAtUtc;
        CreatedAtUtc = startedAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public string PlanCode { get; private set; } = string.Empty;

    public StoreBillingCycle BillingCycle { get; private set; }

    public StoreSubscriptionStatus Status { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? EndsAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static StoreSubscription Create(
        TenantId tenantId,
        string planCode,
        StoreBillingCycle billingCycle,
        DateTimeOffset startedAtUtc,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        return new StoreSubscription(
            StoreSubscriptionId.New(),
            tenantId,
            planCode,
            billingCycle,
            startedAtUtc,
            createdByUserId);
    }

    public void ChangePlan(
        string planCode,
        StoreBillingCycle billingCycle,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        PlanCode = NormalizePlanCode(planCode);
        BillingCycle = billingCycle;
        Status = StoreSubscriptionStatus.Active;
        EndsAtUtc = null;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void Suspend(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == StoreSubscriptionStatus.Suspended)
        {
            return;
        }

        Status = StoreSubscriptionStatus.Suspended;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void Activate(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == StoreSubscriptionStatus.Active)
        {
            return;
        }

        Status = StoreSubscriptionStatus.Active;
        EndsAtUtc = null;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static string NormalizePlanCode(string? planCode)
    {
        var normalized = PlatformPlanCatalog.Normalize(planCode);

        if (!PlatformPlanCatalog.TryGet(normalized, out _))
        {
            throw new ArgumentException(
                "A supported plan code is required.",
                nameof(planCode));
        }

        return normalized;
    }
}
