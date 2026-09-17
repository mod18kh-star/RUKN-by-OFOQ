using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryUserSessionRepository :
    IUserSessionRepository
{
    private readonly List<UserSession> _items = [];

    public IReadOnlyList<UserSession> Items =>
        _items;

    public Task<UserSession?> GetByIdAsync(
        UserSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        var result =
            _items.SingleOrDefault(
                item =>
                    item.Id == sessionId);

        return Task.FromResult(
            result);
    }

    public Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(
        UserId userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserSession> result =
            _items
                .Where(
                    item =>
                        item.UserId == userId &&
                        item.IsUsable(nowUtc))
                .OrderByDescending(
                    item =>
                        item.LastSeenAtUtc)
                .ToArray();

        return Task.FromResult(
            result);
    }

    public Task AddAsync(
        UserSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        _items.Add(session);

        return Task.CompletedTask;
    }
}
