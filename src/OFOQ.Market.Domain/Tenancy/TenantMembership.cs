using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tenancy;

public sealed class TenantMembership :
    Entity<TenantMembershipId>,
    ITenantScoped<TenantId>,
    IAuditable,
    ISoftDeletable
{
    private TenantMembership(
        TenantMembershipId id,
        TenantId tenantId,
        UserId userId,
        TenantRole role,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;

        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    private TenantMembership()
    {
    }

    public TenantId TenantId { get; private set; }

    public UserId UserId { get; private set; }

    public TenantRole Role { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static TenantMembership Create(
        TenantId tenantId,
        UserId userId,
        TenantRole role,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));

        if (userId.IsEmpty)
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));

        return new TenantMembership(
            TenantMembershipId.New(),
            tenantId,
            userId,
            role,
            createdAtUtc,
            createdByUserId);
    }

    public void ChangeRole(
        TenantRole role,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Role == role)
            return;

        Role = role;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void Delete(
        DateTimeOffset deletedAtUtc,
        Guid? deletedByUserId = null)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;
        UpdatedAtUtc = deletedAtUtc;
        UpdatedByUserId = deletedByUserId;
    }
}