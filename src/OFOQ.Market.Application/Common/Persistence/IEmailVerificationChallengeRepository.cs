using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IEmailVerificationChallengeRepository
{
    Task<EmailVerificationChallenge?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailVerificationChallenge>>
        GetActiveByUserIdAsync(
            UserId userId,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default);

    Task AddAsync(
        EmailVerificationChallenge challenge,
        CancellationToken cancellationToken = default);
}
