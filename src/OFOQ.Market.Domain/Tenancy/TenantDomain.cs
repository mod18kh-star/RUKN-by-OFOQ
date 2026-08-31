using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Tenancy;

public sealed class TenantDomain :
    Entity<TenantDomainId>,
    ITenantScoped<TenantId>,
    IAuditable,
    ISoftDeletable
{
    private TenantDomain()
    {
    }

    private TenantDomain(
        TenantDomainId id,
        TenantId tenantId,
        DomainName domain,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId = tenantId;
        Domain = domain;

        Status = TenantDomainStatus.PendingVerification;
        IsPrimary = false;

        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public DomainName Domain { get; private set; }

    public bool IsPrimary { get; private set; }

    public TenantDomainStatus Status { get; private set; }

    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static TenantDomain Create(
        TenantId tenantId,
        string domain,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        return new TenantDomain(
            TenantDomainId.New(),
            tenantId,
            DomainName.Create(domain),
            createdAtUtc,
            createdByUserId);
    }

    public void MarkVerified(
        DateTimeOffset verifiedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == TenantDomainStatus.Disabled)
        {
            throw new InvalidOperationException(
                "A disabled domain cannot be verified.");
        }

        if (Status == TenantDomainStatus.Verified)
            return;

        Status = TenantDomainStatus.Verified;
        VerifiedAtUtc = verifiedAtUtc;

        MarkUpdated(
            verifiedAtUtc,
            updatedByUserId);
    }

    public void MakePrimary(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "A deleted domain cannot become primary.");
        }

        if (Status != TenantDomainStatus.Verified)
        {
            throw new InvalidOperationException(
                "Only a verified domain can become primary.");
        }

        if (IsPrimary)
            return;

        IsPrimary = true;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void RemovePrimary(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (!IsPrimary)
            return;

        IsPrimary = false;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Disable(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == TenantDomainStatus.Disabled)
            return;

        Status = TenantDomainStatus.Disabled;
        IsPrimary = false;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Delete(
        DateTimeOffset deletedAtUtc,
        Guid? deletedByUserId = null)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        IsPrimary = false;

        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;

        MarkUpdated(
            deletedAtUtc,
            deletedByUserId);
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }
}