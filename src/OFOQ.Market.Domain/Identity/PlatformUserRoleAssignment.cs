using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class PlatformUserRoleAssignment :
    Entity<PlatformUserRoleAssignmentId>,
    IAuditable,
    ISoftDeletable
{
    private PlatformUserRoleAssignment(
        PlatformUserRoleAssignmentId id,
        UserId userId,
        PlatformRole role,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        UserId = userId;
        Role = role;

        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    private PlatformUserRoleAssignment()
    {
    }

    public UserId UserId { get; private set; }

    public PlatformRole Role { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static PlatformUserRoleAssignment Create(
        UserId userId,
        PlatformRole role,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (userId.IsEmpty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (role == PlatformRole.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "A valid platform role is required.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "The platform role is not supported.");
        }

        return new PlatformUserRoleAssignment(
            PlatformUserRoleAssignmentId.New(),
            userId,
            role,
            createdAtUtc,
            createdByUserId);
    }

    public void Delete(
        DateTimeOffset deletedAtUtc,
        Guid? deletedByUserId = null)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;

        UpdatedAtUtc = deletedAtUtc;
        UpdatedByUserId = deletedByUserId;
    }
}
