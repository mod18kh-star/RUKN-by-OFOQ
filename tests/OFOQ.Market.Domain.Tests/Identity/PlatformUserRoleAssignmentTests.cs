using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class PlatformUserRoleAssignmentTests
{
    [Fact]
    public void Create_PlatformAdministrator_CreatesActiveAssignment()
    {
        var userId =
            UserId.New();

        var createdByUserId =
            Guid.NewGuid();

        var now =
            new DateTimeOffset(
                2026,
                9,
                12,
                12,
                0,
                0,
                TimeSpan.Zero);

        var assignment =
            PlatformUserRoleAssignment.Create(
                userId,
                PlatformRole.PlatformAdministrator,
                now,
                createdByUserId);

        Assert.False(
            assignment.Id.IsEmpty);

        Assert.Equal(
            userId,
            assignment.UserId);

        Assert.Equal(
            PlatformRole.PlatformAdministrator,
            assignment.Role);

        Assert.Equal(
            now,
            assignment.CreatedAtUtc);

        Assert.Equal(
            createdByUserId,
            assignment.CreatedByUserId);

        Assert.False(
            assignment.IsDeleted);

        Assert.Null(
            assignment.DeletedAtUtc);

        Assert.Null(
            assignment.DeletedByUserId);
    }

    [Fact]
    public void Create_ComplianceReviewer_CreatesAssignment()
    {
        var userId =
            UserId.New();

        var now =
            new DateTimeOffset(
                2026,
                9,
                12,
                12,
                5,
                0,
                TimeSpan.Zero);

        var assignment =
            PlatformUserRoleAssignment.Create(
                userId,
                PlatformRole.ComplianceReviewer,
                now);

        Assert.Equal(
            userId,
            assignment.UserId);

        Assert.Equal(
            PlatformRole.ComplianceReviewer,
            assignment.Role);

        Assert.Equal(
            now,
            assignment.CreatedAtUtc);

        Assert.Null(
            assignment.CreatedByUserId);

        Assert.False(
            assignment.IsDeleted);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () =>
                    PlatformUserRoleAssignment.Create(
                        default,
                        PlatformRole.PlatformAdministrator,
                        DateTimeOffset.UtcNow));

        Assert.Equal(
            "userId",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithUnknownRole_Throws()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    PlatformUserRoleAssignment.Create(
                        UserId.New(),
                        PlatformRole.Unknown,
                        DateTimeOffset.UtcNow));

        Assert.Equal(
            "role",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithUndefinedRole_Throws()
    {
        var undefinedRole =
            (PlatformRole)999;

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    PlatformUserRoleAssignment.Create(
                        UserId.New(),
                        undefinedRole,
                        DateTimeOffset.UtcNow));

        Assert.Equal(
            "role",
            exception.ParamName);
    }

    [Fact]
    public void Delete_MarksAssignmentDeletedAndAudited()
    {
        var assignment =
            PlatformUserRoleAssignment.Create(
                UserId.New(),
                PlatformRole.PlatformAdministrator,
                new DateTimeOffset(
                    2026,
                    9,
                    12,
                    12,
                    0,
                    0,
                    TimeSpan.Zero));

        var deletedByUserId =
            Guid.NewGuid();

        var deletedAtUtc =
            new DateTimeOffset(
                2026,
                9,
                12,
                13,
                0,
                0,
                TimeSpan.Zero);

        assignment.Delete(
            deletedAtUtc,
            deletedByUserId);

        Assert.True(
            assignment.IsDeleted);

        Assert.Equal(
            deletedAtUtc,
            assignment.DeletedAtUtc);

        Assert.Equal(
            deletedByUserId,
            assignment.DeletedByUserId);

        Assert.Equal(
            deletedAtUtc,
            assignment.UpdatedAtUtc);

        Assert.Equal(
            deletedByUserId,
            assignment.UpdatedByUserId);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_IsIdempotent()
    {
        var assignment =
            PlatformUserRoleAssignment.Create(
                UserId.New(),
                PlatformRole.ComplianceReviewer,
                new DateTimeOffset(
                    2026,
                    9,
                    12,
                    12,
                    0,
                    0,
                    TimeSpan.Zero));

        var firstActor =
            Guid.NewGuid();

        var firstDeletedAt =
            new DateTimeOffset(
                2026,
                9,
                12,
                13,
                0,
                0,
                TimeSpan.Zero);

        assignment.Delete(
            firstDeletedAt,
            firstActor);

        assignment.Delete(
            firstDeletedAt.AddHours(1),
            Guid.NewGuid());

        Assert.True(
            assignment.IsDeleted);

        Assert.Equal(
            firstDeletedAt,
            assignment.DeletedAtUtc);

        Assert.Equal(
            firstActor,
            assignment.DeletedByUserId);

        Assert.Equal(
            firstDeletedAt,
            assignment.UpdatedAtUtc);

        Assert.Equal(
            firstActor,
            assignment.UpdatedByUserId);
    }

    [Fact]
    public void AssignmentId_FromEmptyGuid_Throws()
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () =>
                    PlatformUserRoleAssignmentId.From(
                        Guid.Empty));

        Assert.Equal(
            "value",
            exception.ParamName);
    }

    [Fact]
    public void AssignmentId_New_CreatesNonEmptyId()
    {
        var id =
            PlatformUserRoleAssignmentId.New();

        Assert.False(
            id.IsEmpty);

        Assert.NotEqual(
            Guid.Empty,
            id.Value);
    }
}
