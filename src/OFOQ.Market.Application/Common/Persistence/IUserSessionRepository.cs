using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(
        UserSessionId sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(
        UserId userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserSession session,
        CancellationToken cancellationToken = default);
}
