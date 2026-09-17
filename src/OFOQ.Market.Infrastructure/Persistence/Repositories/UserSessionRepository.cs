using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class UserSessionRepository :
    IUserSessionRepository
{
    private readonly MarketDbContext _dbContext;

    public UserSessionRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserSession?> GetByIdAsync(
        UserSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSessions
            .SingleOrDefaultAsync(
                session =>
                    session.Id == sessionId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(
        UserId userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserSessions
            .Where(
                session =>
                    session.UserId == userId &&
                    session.RevokedAtUtc == null &&
                    session.ExpiresAtUtc > nowUtc)
            .OrderByDescending(
                session =>
                    session.LastSeenAtUtc)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        UserSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await _dbContext.UserSessions.AddAsync(
            session,
            cancellationToken);
    }
}
