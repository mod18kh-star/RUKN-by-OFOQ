using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Identity;

public sealed class MfaLoginChallengePersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Database_PersistsValidMfaLoginChallenge()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                "TOKEN-HASH-ONE",
                now.AddMinutes(5),
                now,
                user.Id.Value);

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.Users.Add(
                user);

            dbContext.MfaLoginChallenges.Add(
                challenge);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext
                .MfaLoginChallenges
                .SingleAsync();

        Assert.Equal(
            user.Id,
            saved.UserId);

        Assert.Equal(
            "TOKEN-HASH-ONE",
            saved.TokenHash);

        Assert.Equal(
            0,
            saved.FailedAttemptCount);

        Assert.False(
            saved.IsConsumed);

        Assert.False(
            saved.IsRevoked);

        Assert.False(
            saved.IsExhausted);
    }

    [Fact]
    public async Task Database_RejectsChallengeForNonExistingUser()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var challenge =
            MfaLoginChallenge.Create(
                UserId.New(),
                "TOKEN-HASH-ONE",
                now.AddMinutes(5),
                now);

        await using var dbContext =
            _database.CreateContext();

        dbContext.MfaLoginChallenges.Add(
            challenge);

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
            "fk_mfa_login_challenges_users_user_id",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsDuplicateTokenHash()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var firstUser =
            User.Create(
                "first@example.com",
                "HASHED-PASSWORD",
                now);

        var secondUser =
            User.Create(
                "second@example.com",
                "HASHED-PASSWORD",
                now);

        var firstChallenge =
            MfaLoginChallenge.Create(
                firstUser.Id,
                "SAME-TOKEN-HASH",
                now.AddMinutes(5),
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Users.AddRange(
                firstUser,
                secondUser);

            setupContext.MfaLoginChallenges.Add(
                firstChallenge);

            await setupContext.SaveChangesAsync();
        }

        var secondChallenge =
            MfaLoginChallenge.Create(
                secondUser.Id,
                "SAME-TOKEN-HASH",
                now.AddMinutes(5),
                now);

        await using var dbContext =
            _database.CreateContext();

        dbContext.MfaLoginChallenges.Add(
            secondChallenge);

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
            "ux_mfa_login_challenges_token_hash",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_PersistsFailedAttemptCount()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                "TOKEN-HASH-ONE",
                now.AddMinutes(5),
                now);

        challenge.RegisterFailedAttempt(
            now.AddSeconds(10));

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.Users.Add(
                user);

            dbContext.MfaLoginChallenges.Add(
                challenge);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext
                .MfaLoginChallenges
                .SingleAsync();

        Assert.Equal(
            1,
            saved.FailedAttemptCount);

        Assert.False(
            saved.IsExhausted);
    }

    [Fact]
    public async Task Database_PersistsConsumedChallenge()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                "TOKEN-HASH-ONE",
                now.AddMinutes(5),
                now);

        challenge.Consume(
            now.AddMinutes(1));

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.Users.Add(
                user);

            dbContext.MfaLoginChallenges.Add(
                challenge);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext
                .MfaLoginChallenges
                .SingleAsync();

        Assert.True(
            saved.IsConsumed);

        Assert.NotNull(
            saved.ConsumedAtUtc);

        Assert.False(
            saved.IsUsable(
                now.AddMinutes(2)));
    }

    [Fact]
    public async Task Database_PersistsRevokedChallenge()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                "TOKEN-HASH-ONE",
                now.AddMinutes(5),
                now);

        challenge.Revoke(
            now.AddMinutes(1));

        await using (var dbContext =
                     _database.CreateContext())
        {
            dbContext.Users.Add(
                user);

            dbContext.MfaLoginChallenges.Add(
                challenge);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var saved =
            await verificationContext
                .MfaLoginChallenges
                .SingleAsync();

        Assert.True(
            saved.IsRevoked);

        Assert.NotNull(
            saved.RevokedAtUtc);

        Assert.False(
            saved.IsUsable(
                now.AddMinutes(2)));
    }

    [Fact]
    public async Task Database_RejectsConcurrentChallengeConsumption()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                "TOKEN-HASH-ONE",
                now.AddMinutes(5),
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Users.Add(
                user);

            setupContext.MfaLoginChallenges.Add(
                challenge);

            await setupContext.SaveChangesAsync();
        }

        await using var firstContext =
            _database.CreateContext();

        await using var secondContext =
            _database.CreateContext();

        var firstCopy =
            await firstContext
                .MfaLoginChallenges
                .SingleAsync();

        var secondCopy =
            await secondContext
                .MfaLoginChallenges
                .SingleAsync();

        firstCopy.Consume(
            now.AddMinutes(1));

        secondCopy.Consume(
            now.AddMinutes(2));

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Repository_ReturnsOnlyActiveChallenges()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(now);

        var active =
            MfaLoginChallenge.Create(
                user.Id,
                "ACTIVE-HASH",
                now.AddMinutes(5),
                now);

        var consumed =
            MfaLoginChallenge.Create(
                user.Id,
                "CONSUMED-HASH",
                now.AddMinutes(5),
                now);

        consumed.Consume(
            now.AddSeconds(10));

        var revoked =
            MfaLoginChallenge.Create(
                user.Id,
                "REVOKED-HASH",
                now.AddMinutes(5),
                now);

        revoked.Revoke(
            now.AddSeconds(10));

        var expired =
            MfaLoginChallenge.Create(
                user.Id,
                "EXPIRED-HASH",
                now.AddMinutes(-1),
                now.AddMinutes(-10));

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Users.Add(
                user);

            setupContext.MfaLoginChallenges.AddRange(
                active,
                consumed,
                revoked,
                expired);

            await setupContext.SaveChangesAsync();
        }

        await using var dbContext =
            _database.CreateContext();

        var repository =
            new Persistence.Repositories
                .MfaLoginChallengeRepository(
                    dbContext);

        var results =
            await repository
                .GetActiveByUserIdAsync(
                    user.Id,
                    now);

        var saved =
            Assert.Single(
                results);

        Assert.Equal(
            "ACTIVE-HASH",
            saved.TokenHash);
    }

    private static User CreateUser(
        DateTimeOffset now)
    {
        return User.Create(
            "user@example.com",
            "HASHED-PASSWORD",
            now);
    }
}