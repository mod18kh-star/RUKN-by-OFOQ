using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Identity;

public sealed class PlatformAuthorizationPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Database_PlatformRoleAssignment_PersistsWithoutTenantContext()
    {
        await _database.ResetAsync();

        var user =
            await CreateUserAsync();

        var assignment =
            PlatformUserRoleAssignment.Create(
                user.Id,
                PlatformRole.PlatformAdministrator,
                DateTimeOffset.UtcNow,
                user.Id.Value);

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.PlatformUserRoleAssignments.Add(
                assignment);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext
                .PlatformUserRoleAssignments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            user.Id,
            saved.UserId);

        Assert.Equal(
            PlatformRole.PlatformAdministrator,
            saved.Role);

        Assert.False(
            saved.IsDeleted);
    }

    [Fact]
    public async Task Database_UserCanHoldMultipleDifferentPlatformRoles()
    {
        await _database.ResetAsync();

        var user =
            await CreateUserAsync();

        var now =
            DateTimeOffset.UtcNow;

        await using var dbContext =
            _database.CreateContext();

        dbContext.PlatformUserRoleAssignments.AddRange(
            PlatformUserRoleAssignment.Create(
                user.Id,
                PlatformRole.PlatformAdministrator,
                now,
                user.Id.Value),
            PlatformUserRoleAssignment.Create(
                user.Id,
                PlatformRole.ComplianceReviewer,
                now.AddSeconds(1),
                user.Id.Value));

        await dbContext.SaveChangesAsync();

        var roles =
            await dbContext
                .PlatformUserRoleAssignments
                .AsNoTracking()
                .Where(
                    assignment =>
                        assignment.UserId == user.Id)
                .OrderBy(
                    assignment =>
                        assignment.Role)
                .Select(
                    assignment =>
                        assignment.Role)
                .ToArrayAsync();

        Assert.Equal(
            2,
            roles.Length);

        Assert.Contains(
            PlatformRole.PlatformAdministrator,
            roles);

        Assert.Contains(
            PlatformRole.ComplianceReviewer,
            roles);
    }

    [Fact]
    public async Task Database_DuplicateActiveRoleForSameUser_IsRejected()
    {
        await _database.ResetAsync();

        var user =
            await CreateUserAsync();

        var now =
            DateTimeOffset.UtcNow;

        await using (var firstContext =
                     _database.CreateContext())
        {
            firstContext.PlatformUserRoleAssignments.Add(
                PlatformUserRoleAssignment.Create(
                    user.Id,
                    PlatformRole.PlatformAdministrator,
                    now,
                    user.Id.Value));

            await firstContext.SaveChangesAsync();
        }

        await using var duplicateContext =
            _database.CreateContext();

        duplicateContext.PlatformUserRoleAssignments.Add(
            PlatformUserRoleAssignment.Create(
                user.Id,
                PlatformRole.PlatformAdministrator,
                now.AddSeconds(1),
                user.Id.Value));

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () =>
                    duplicateContext.SaveChangesAsync());

        AssertPostgresConstraint(
            exception,
            "23505",
            "ux_platform_user_role_assignments_user_role_active");
    }

    [Fact]
    public async Task Database_SoftDeletedRole_CanBeGrantedAgain()
    {
        await _database.ResetAsync();

        var user =
            await CreateUserAsync();

        var now =
            DateTimeOffset.UtcNow;

        PlatformUserRoleAssignmentId
            originalAssignmentId;

        await using (var setupContext =
                     _database.CreateContext())
        {
            var original =
                PlatformUserRoleAssignment.Create(
                    user.Id,
                    PlatformRole.ComplianceReviewer,
                    now,
                    user.Id.Value);

            originalAssignmentId =
                original.Id;

            setupContext.PlatformUserRoleAssignments.Add(
                original);

            await setupContext.SaveChangesAsync();
        }

        await using (var deleteContext =
                     _database.CreateContext())
        {
            var original =
                await deleteContext
                    .PlatformUserRoleAssignments
                    .SingleAsync(
                        assignment =>
                            assignment.Id ==
                            originalAssignmentId);

            original.Delete(
                now.AddMinutes(1),
                user.Id.Value);

            await deleteContext.SaveChangesAsync();
        }

        var replacement =
            PlatformUserRoleAssignment.Create(
                user.Id,
                PlatformRole.ComplianceReviewer,
                now.AddMinutes(2),
                user.Id.Value);

        await using (var regrantContext =
                     _database.CreateContext())
        {
            regrantContext.PlatformUserRoleAssignments.Add(
                replacement);

            await regrantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var activeAssignments =
            await verificationContext
                .PlatformUserRoleAssignments
                .AsNoTracking()
                .Where(
                    assignment =>
                        assignment.UserId ==
                        user.Id &&
                        assignment.Role ==
                        PlatformRole.ComplianceReviewer)
                .ToArrayAsync();

        var active =
            Assert.Single(
                activeAssignments);

        Assert.Equal(
            replacement.Id,
            active.Id);

        var completeHistory =
            await verificationContext
                .PlatformUserRoleAssignments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    assignment =>
                        assignment.UserId ==
                        user.Id &&
                        assignment.Role ==
                        PlatformRole.ComplianceReviewer)
                .OrderBy(
                    assignment =>
                        assignment.CreatedAtUtc)
                .ToArrayAsync();

        Assert.Equal(
            2,
            completeHistory.Length);

        Assert.Single(
            completeHistory,
            assignment =>
                assignment.IsDeleted);

        Assert.Single(
            completeHistory,
            assignment =>
                !assignment.IsDeleted);
    }

    [Fact]
    public async Task Database_SoftDeletedAssignment_IsHiddenByDefaultQueryFilter()
    {
        await _database.ResetAsync();

        var user =
            await CreateUserAsync();

        var now =
            DateTimeOffset.UtcNow;

        var assignment =
            PlatformUserRoleAssignment.Create(
                user.Id,
                PlatformRole.PlatformAdministrator,
                now,
                user.Id.Value);

        assignment.Delete(
            now.AddMinutes(1),
            user.Id.Value);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.PlatformUserRoleAssignments.Add(
                assignment);

            await setupContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        Assert.Empty(
            await verificationContext
                .PlatformUserRoleAssignments
                .AsNoTracking()
                .ToArrayAsync());

        var historical =
            await verificationContext
                .PlatformUserRoleAssignments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync();

        Assert.True(
            historical.IsDeleted);

        Assert.NotNull(
            historical.DeletedAtUtc);
    }

    [Fact]
    public async Task Database_UnknownPlatformRole_IsRejected()
    {
        await _database.ResetAsync();

        var user =
            await CreateUserAsync();

        await using var dbContext =
            _database.CreateContext();

        var id =
            Guid.NewGuid();

        var now =
            DateTimeOffset.UtcNow;

        var invalidRole =
            "Unknown";

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    dbContext.Database
                        .ExecuteSqlInterpolatedAsync(
                            $"""
                            INSERT INTO platform_user_role_assignments
                            (
                                id,
                                user_id,
                                role,
                                created_at_utc,
                                is_deleted
                            )
                            VALUES
                            (
                                {id},
                                {user.Id.Value},
                                {invalidRole},
                                {now},
                                FALSE
                            );
                            """));

        AssertPostgresConstraint(
            exception,
            "23514",
            "ck_platform_user_role_assignments_role");
    }

    [Fact]
    public async Task Database_DeletedAssignmentWithoutDeletionTimestamp_IsRejected()
    {
        await _database.ResetAsync();

        var user =
            await CreateUserAsync();

        await using var dbContext =
            _database.CreateContext();

        var id =
            Guid.NewGuid();

        var now =
            DateTimeOffset.UtcNow;

        var role =
            nameof(
                PlatformRole.PlatformAdministrator);

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    dbContext.Database
                        .ExecuteSqlInterpolatedAsync(
                            $"""
                            INSERT INTO platform_user_role_assignments
                            (
                                id,
                                user_id,
                                role,
                                created_at_utc,
                                is_deleted,
                                deleted_at_utc
                            )
                            VALUES
                            (
                                {id},
                                {user.Id.Value},
                                {role},
                                {now},
                                TRUE,
                                NULL
                            );
                            """));

        AssertPostgresConstraint(
            exception,
            "23514",
            "ck_platform_user_role_assignments_deleted_audit");
    }

    [Fact]
    public async Task Database_AssignmentForUnknownUser_IsRejected()
    {
        await _database.ResetAsync();

        var missingUserId =
            UserId.New();

        var assignment =
            PlatformUserRoleAssignment.Create(
                missingUserId,
                PlatformRole.PlatformAdministrator,
                DateTimeOffset.UtcNow);

        await using var dbContext =
            _database.CreateContext();

        dbContext.PlatformUserRoleAssignments.Add(
            assignment);

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () =>
                    dbContext.SaveChangesAsync());

        AssertPostgresConstraint(
            exception,
            "23503",
            "fk_platform_user_role_assignments_users_user_id");
    }

    private async Task<User> CreateUserAsync()
    {
        var user =
            User.Create(
                $"platform-{Guid.NewGuid():N}@example.com",
                "integration-test-password-hash",
                DateTimeOffset.UtcNow);

        await using var dbContext =
            _database.CreateContext();

        dbContext.Users.Add(
            user);

        await dbContext.SaveChangesAsync();

        return user;
    }

    private static void AssertPostgresConstraint(
        Exception exception,
        string expectedSqlState,
        string expectedConstraintName)
    {
        var postgresException =
            FindPostgresException(
                exception);

        Assert.NotNull(
            postgresException);

        Assert.Equal(
            expectedSqlState,
            postgresException.SqlState);

        Assert.Equal(
            expectedConstraintName,
            postgresException.ConstraintName);
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        Exception? current =
            exception;

        while (current is not null)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

            current =
                current.InnerException;
        }

        return null;
    }
}
