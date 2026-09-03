using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IMfaLoginChallengeRepository
{
    Task<MfaLoginChallenge?> GetByIdAsync(
        MfaLoginChallengeId challengeId,
        CancellationToken cancellationToken = default);

    Task<MfaLoginChallenge?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MfaLoginChallenge>> GetActiveByUserIdAsync(
        UserId userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MfaLoginChallenge challenge,
        CancellationToken cancellationToken = default);
}