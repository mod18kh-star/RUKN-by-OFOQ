using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class EmailVerificationChallengeRepository :
    IEmailVerificationChallengeRepository
{
    private readonly MarketDbContext
        _dbContext;

    public EmailVerificationChallengeRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<EmailVerificationChallenge?>
        GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        return _dbContext
            .EmailVerificationChallenges
            .SingleOrDefaultAsync(
                challenge =>
                    challenge.TokenHash ==
                        tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyList<EmailVerificationChallenge>>
        GetActiveByUserIdAsync(
            UserId userId,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .EmailVerificationChallenges
            .Where(
                challenge =>
                    challenge.UserId ==
                        userId &&
                    challenge.ConsumedAtUtc ==
                        null &&
                    challenge.RevokedAtUtc ==
                        null &&
                    challenge.ExpiresAtUtc >
                        nowUtc)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        EmailVerificationChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            challenge);

        await _dbContext
            .EmailVerificationChallenges
            .AddAsync(
                challenge,
                cancellationToken);
    }
}
