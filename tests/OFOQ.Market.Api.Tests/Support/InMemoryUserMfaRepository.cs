using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryUserMfaRepository :
    IUserMfaRepository
{
    private readonly List<UserMfa> _items = [];

    public IReadOnlyList<UserMfa> Items =>
        _items;

    public Task<UserMfa?> GetByIdAsync(
        UserMfaId userMfaId,
        CancellationToken cancellationToken = default)
    {
        var result =
            _items.SingleOrDefault(
                item =>
                    item.Id ==
                    userMfaId);

        return Task.FromResult(
            result);
    }

    public Task<UserMfa?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var result =
            _items.SingleOrDefault(
                item =>
                    item.UserId ==
                    userId);

        return Task.FromResult(
            result);
    }

    public Task<bool> ExistsForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var exists =
            _items.Any(
                item =>
                    item.UserId ==
                    userId);

        return Task.FromResult(
            exists);
    }

    public Task AddAsync(
        UserMfa userMfa,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            userMfa);

        _items.Add(
            userMfa);

        return Task.CompletedTask;
    }
}