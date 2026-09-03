using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Identity;

public sealed class UserMfaRecoveryCodePersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Database_PersistsValidRecoveryCode()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var mfa =
            CreateEnabledMfa(
                user,
                now);

        var recoveryCode =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "HASH-ONE",
                now,
                user.Id.Value);

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.Users.Add(user);

            dbContext.UserMfas.Add(mfa);

            dbContext.UserMfaRecoveryCodes.Add(
                recoveryCode);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext
                .UserMfaRecoveryCodes
                .SingleAsync();

        Assert.Equal(
            mfa.Id,
            saved.UserMfaId);

        Assert.Equal(
            "HASH-ONE",
            saved.CodeHash);

        Assert.False(
            saved.IsUsed);

        Assert.Null(
            saved.UsedAtUtc);
    }

    [Fact]
    public async Task Database_RejectsRecoveryCodeForNonExistingMfa()
    {
        await _database.ResetAsync();

        var recoveryCode =
            UserMfaRecoveryCode.Create(
                UserMfaId.New(),
                "HASH-ONE",
                DateTimeOffset.UtcNow);

        await using var dbContext =
            _database.CreateContext();

        dbContext.UserMfaRecoveryCodes.Add(
            recoveryCode);

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
            "fk_user_mfa_recovery_codes_user_mfa_id",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsDuplicateHashForSameMfa()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var mfa =
            CreateEnabledMfa(
                user,
                now);

        var first =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "SAME-HASH",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Users.Add(user);

            setupContext.UserMfas.Add(mfa);

            setupContext.UserMfaRecoveryCodes.Add(
                first);

            await setupContext.SaveChangesAsync();
        }

        var second =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "SAME-HASH",
                now.AddSeconds(1));

        await using var dbContext =
            _database.CreateContext();

        dbContext.UserMfaRecoveryCodes.Add(
            second);

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
            "ux_user_mfa_recovery_codes_mfa_hash",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_PersistsUsedRecoveryCode()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var mfa =
            CreateEnabledMfa(
                user,
                now);

        var expectedUsedAtUtc =
            now.AddMinutes(1);

        var recoveryCode =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "HASH-ONE",
                now);

        recoveryCode.MarkUsed(
            expectedUsedAtUtc,
            user.Id.Value);

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.Users.Add(user);

            dbContext.UserMfas.Add(mfa);

            dbContext.UserMfaRecoveryCodes.Add(
                recoveryCode);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext
                .UserMfaRecoveryCodes
                .SingleAsync();

        Assert.True(
            saved.IsUsed);

        Assert.NotNull(
            saved.UsedAtUtc);

        var timestampDifference =
            (
                saved.UsedAtUtc.Value -
                expectedUsedAtUtc
            )
            .Duration();

        Assert.True(
            timestampDifference <=
                TimeSpan.FromMicroseconds(1),
            $"Expected UsedAtUtc within 1 microsecond. Actual difference: {timestampDifference}.");
    }

    [Fact]
    public async Task Database_RejectsConcurrentDoubleUse()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var mfa =
            CreateEnabledMfa(
                user,
                now);

        var recoveryCode =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "HASH-ONE",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Users.Add(user);

            setupContext.UserMfas.Add(mfa);

            setupContext.UserMfaRecoveryCodes.Add(
                recoveryCode);

            await setupContext.SaveChangesAsync();
        }

        await using var firstContext =
            _database.CreateContext();

        await using var secondContext =
            _database.CreateContext();

        var firstCopy =
            await firstContext
                .UserMfaRecoveryCodes
                .SingleAsync();

        var secondCopy =
            await secondContext
                .UserMfaRecoveryCodes
                .SingleAsync();

        firstCopy.MarkUsed(
            now.AddMinutes(1));

        secondCopy.MarkUsed(
            now.AddMinutes(2));

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    secondContext.SaveChangesAsync());
    }

    private static User CreateUser(
        DateTimeOffset now)
    {
        return User.Create(
            "user@example.com",
            "HASHED-PASSWORD",
            now);
    }

    private static UserMfa CreateEnabledMfa(
        User user,
        DateTimeOffset now)
    {
        var mfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED-SECRET",
                now);

        mfa.ConfirmEnrollment(
            100,
            now);

        return mfa;
    }
}