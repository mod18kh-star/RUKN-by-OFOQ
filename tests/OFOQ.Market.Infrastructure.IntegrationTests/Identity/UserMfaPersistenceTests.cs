using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Identity;

public sealed class UserMfaPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Database_PersistsValidUserMfa()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Create(
                "user@example.com",
                "HASHED-PASSWORD",
                now);

        var mfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED-SECRET",
                now);

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.Users.Add(
                user);

            dbContext.UserMfas.Add(
                mfa);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext.UserMfas
                .SingleAsync();

        Assert.Equal(
            user.Id,
            saved.UserId);

        Assert.Equal(
            "PROTECTED-SECRET",
            saved.ProtectedSecret);

        Assert.Equal(
            UserMfaStatus.PendingEnrollment,
            saved.Status);

        Assert.Null(
            saved.LastAcceptedTimeStep);
    }

    [Fact]
    public async Task Database_RejectsDuplicateMfaForSameUser()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Create(
                "user@example.com",
                "HASHED-PASSWORD",
                now);

        var firstMfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED-SECRET-1",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Users.Add(
                user);

            setupContext.UserMfas.Add(
                firstMfa);

            await setupContext.SaveChangesAsync();
        }

        var secondMfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED-SECRET-2",
                now.AddSeconds(1));

        await using var dbContext =
            _database.CreateContext();

        dbContext.UserMfas.Add(
            secondMfa);

        var exception =
            await Assert.ThrowsAsync<
                DbUpdateException>(
                    () =>
                        dbContext.SaveChangesAsync());

        var postgresException =
            Assert.IsType<PostgresException>(
                exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            postgresException.SqlState);

        Assert.Equal(
            "ux_user_mfa_user_id",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsMfaForNonExistingUser()
    {
        await _database.ResetAsync();

        var mfa =
            UserMfa.BeginEnrollment(
                UserId.New(),
                "PROTECTED-SECRET",
                DateTimeOffset.UtcNow);

        await using var dbContext =
            _database.CreateContext();

        dbContext.UserMfas.Add(
            mfa);

        var exception =
            await Assert.ThrowsAsync<
                DbUpdateException>(
                    () =>
                        dbContext.SaveChangesAsync());

        var postgresException =
            Assert.IsType<PostgresException>(
                exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            postgresException.SqlState);

        Assert.Equal(
            "fk_user_mfa_users_user_id",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_PersistsLastAcceptedTotpTimeStep()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Create(
                "user@example.com",
                "HASHED-PASSWORD",
                now);

        var mfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED-SECRET",
                now);

        mfa.Enable(
            now);

        mfa.AcceptTimeStep(
            987654,
            now.AddSeconds(1));

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.Users.Add(
                user);

            dbContext.UserMfas.Add(
                mfa);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext.UserMfas
                .SingleAsync();

        Assert.Equal(
            UserMfaStatus.Enabled,
            saved.Status);

        Assert.Equal(
            987654,
            saved.LastAcceptedTimeStep);

        Assert.NotNull(
            saved.EnabledAtUtc);
    }
}