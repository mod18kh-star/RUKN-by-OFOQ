using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryMfaLoginChallengeRepository :
    IMfaLoginChallengeRepository
{
    private readonly List<MfaLoginChallenge> _items = [];

    public IReadOnlyList<MfaLoginChallenge> Items =>
        _items;

    public Task<MfaLoginChallenge?> GetByIdAsync(
        MfaLoginChallengeId challengeId,
        CancellationToken cancellationToken = default)
    {
        var result =
            _items.SingleOrDefault(
                item =>
                    item.Id ==
                    challengeId);

        return Task.FromResult(
            result);
    }

    public Task<MfaLoginChallenge?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var result =
            _items.SingleOrDefault(
                item =>
                    item.TokenHash ==
                    tokenHash);

        return Task.FromResult(
            result);
    }

    public Task<IReadOnlyList<MfaLoginChallenge>>
        GetActiveByUserIdAsync(
            UserId userId,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MfaLoginChallenge> result =
            _items
                .Where(
                    item =>
                        item.UserId ==
                            userId &&
                        item.IsUsable(
                            nowUtc))
                .ToArray();

        return Task.FromResult(
            result);
    }

    public Task AddAsync(
        MfaLoginChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            challenge);

        _items.Add(
            challenge);

        return Task.CompletedTask;
    }
}