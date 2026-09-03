using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class MfaLoginChallengeRepository :
    IMfaLoginChallengeRepository
{
    private readonly MarketDbContext _dbContext;

    public MfaLoginChallengeRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<MfaLoginChallenge?> GetByIdAsync(
        MfaLoginChallengeId challengeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MfaLoginChallenges
            .SingleOrDefaultAsync(
                challenge =>
                    challenge.Id ==
                    challengeId,
                cancellationToken);
    }

    public Task<MfaLoginChallenge?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        return _dbContext.MfaLoginChallenges
            .SingleOrDefaultAsync(
                challenge =>
                    challenge.TokenHash ==
                    tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyList<MfaLoginChallenge>>
        GetActiveByUserIdAsync(
            UserId userId,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .MfaLoginChallenges
            .Where(
                challenge =>
                    challenge.UserId ==
                        userId &&
                    challenge.ConsumedAtUtc ==
                        null &&
                    challenge.RevokedAtUtc ==
                        null &&
                    challenge.FailedAttemptCount <
                        MfaLoginChallenge.MaximumFailedAttempts &&
                    challenge.ExpiresAtUtc >
                        nowUtc)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        MfaLoginChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            challenge);

        await _dbContext.MfaLoginChallenges
            .AddAsync(
                challenge,
                cancellationToken);
    }
}