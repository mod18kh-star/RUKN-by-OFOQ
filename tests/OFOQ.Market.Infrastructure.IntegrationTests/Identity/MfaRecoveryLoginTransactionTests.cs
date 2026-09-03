using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Identity;

public sealed class MfaRecoveryLoginTransactionTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Transaction_CommitsRecoveryCodeAndChallengeTogether()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(
                now);

        var mfa =
            CreateEnabledMfa(
                user,
                now);

        var recoveryCode =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "RECOVERY-HASH-ONE",
                now,
                user.Id.Value);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                "CHALLENGE-HASH-ONE",
                now.AddMinutes(5),
                now,
                user.Id.Value);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Users.Add(
                user);

            setupContext.UserMfas.Add(
                mfa);

            setupContext.UserMfaRecoveryCodes.Add(
                recoveryCode);

            setupContext.MfaLoginChallenges.Add(
                challenge);

            await setupContext.SaveChangesAsync();
        }

        await using (var dbContext =
                     _database.CreateContext())
        {
            var recoveryCodeRepository =
                new UserMfaRecoveryCodeRepository(
                    dbContext);

            var transactionExecutor =
                new EfTransactionExecutor(
                    dbContext);

            var succeeded =
                await transactionExecutor.ExecuteAsync(
                    async cancellationToken =>
                    {
                        var savedChallenge =
                            await dbContext
                                .MfaLoginChallenges
                                .SingleAsync(
                                    value =>
                                        value.TokenHash ==
                                        "CHALLENGE-HASH-ONE",
                                    cancellationToken);

                        var recoveryCodeConsumed =
                            await recoveryCodeRepository
                                .TryConsumeByHashAsync(
                                    mfa.Id,
                                    "RECOVERY-HASH-ONE",
                                    now.AddMinutes(1),
                                    user.Id.Value,
                                    cancellationToken);

                        if (!recoveryCodeConsumed)
                        {
                            throw new InvalidOperationException(
                                "The recovery code could not be consumed.");
                        }

                        savedChallenge.Consume(
                            now.AddMinutes(1),
                            user.Id.Value);

                        await dbContext.SaveChangesAsync(
                            cancellationToken);

                        return true;
                    });

            Assert.True(
                succeeded);
        }

        await using var verificationContext =
            _database.CreateContext();

        var savedRecoveryCode =
            await verificationContext
                .UserMfaRecoveryCodes
                .SingleAsync();

        var savedChallengeAfterCommit =
            await verificationContext
                .MfaLoginChallenges
                .SingleAsync();

        Assert.True(
            savedRecoveryCode.IsUsed);

        Assert.NotNull(
            savedRecoveryCode.UsedAtUtc);

        Assert.True(
            savedChallengeAfterCommit.IsConsumed);

        Assert.NotNull(
            savedChallengeAfterCommit.ConsumedAtUtc);
    }

    [Fact]
    public async Task Transaction_RollsBackBothChanges_WhenLaterStageFails()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            CreateUser(
                now);

        var mfa =
            CreateEnabledMfa(
                user,
                now);

        var recoveryCode =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "RECOVERY-HASH-ROLLBACK",
                now,
                user.Id.Value);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                "CHALLENGE-HASH-ROLLBACK",
                now.AddMinutes(5),
                now,
                user.Id.Value);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Users.Add(
                user);

            setupContext.UserMfas.Add(
                mfa);

            setupContext.UserMfaRecoveryCodes.Add(
                recoveryCode);

            setupContext.MfaLoginChallenges.Add(
                challenge);

            await setupContext.SaveChangesAsync();
        }

        await using (var dbContext =
                     _database.CreateContext())
        {
            var recoveryCodeRepository =
                new UserMfaRecoveryCodeRepository(
                    dbContext);

            var transactionExecutor =
                new EfTransactionExecutor(
                    dbContext);

           var exception =
    await Assert.ThrowsAsync<
        InvalidOperationException>(
            () =>
                transactionExecutor.ExecuteAsync<bool>(
                    async cancellationToken =>
                    {
                        var savedChallenge =
                            await dbContext
                                .MfaLoginChallenges
                                .SingleAsync(
                                    value =>
                                        value.TokenHash ==
                                        "CHALLENGE-HASH-ROLLBACK",
                                    cancellationToken);

                        var recoveryCodeConsumed =
                            await recoveryCodeRepository
                                .TryConsumeByHashAsync(
                                    mfa.Id,
                                    "RECOVERY-HASH-ROLLBACK",
                                    now.AddMinutes(1),
                                    user.Id.Value,
                                    cancellationToken);

                        if (!recoveryCodeConsumed)
                        {
                            throw new InvalidOperationException(
                                "The recovery code could not be consumed.");
                        }

                        savedChallenge.Consume(
                            now.AddMinutes(1),
                            user.Id.Value);

                        await dbContext.SaveChangesAsync(
                            cancellationToken);

                        throw new InvalidOperationException(
                            "FORCED-ROLLBACK");
                    }));

            Assert.Equal(
                "FORCED-ROLLBACK",
                exception.Message);
        }

        await using var verificationContext =
            _database.CreateContext();

        var savedRecoveryCode =
            await verificationContext
                .UserMfaRecoveryCodes
                .SingleAsync();

        var savedChallengeAfterRollback =
            await verificationContext
                .MfaLoginChallenges
                .SingleAsync();

        /*
         * The recovery code must NOT have been burned.
         */
        Assert.False(
            savedRecoveryCode.IsUsed);

        Assert.Null(
            savedRecoveryCode.UsedAtUtc);

        /*
         * The MFA challenge must also remain unused.
         */
        Assert.False(
            savedChallengeAfterRollback.IsConsumed);

        Assert.Null(
            savedChallengeAfterRollback.ConsumedAtUtc);
    }

    private static User CreateUser(
        DateTimeOffset now)
    {
        return User.Create(
            "recovery-transaction@example.com",
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