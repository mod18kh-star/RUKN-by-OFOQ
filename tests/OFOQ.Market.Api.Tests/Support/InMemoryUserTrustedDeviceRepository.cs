using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryUserTrustedDeviceRepository :
    IUserTrustedDeviceRepository
{
    private readonly List<UserTrustedDevice> _items = [];

    public IReadOnlyList<UserTrustedDevice> Items =>
        _items;

    public Task<UserTrustedDevice?> GetByIdAsync(
        UserTrustedDeviceId id,
        CancellationToken cancellationToken = default)
    {
        var result =
            _items.SingleOrDefault(
                item =>
                    item.Id == id);

        return Task.FromResult(
            result);
    }

    public Task<IReadOnlyList<UserTrustedDevice>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserTrustedDevice> result =
            _items
                .Where(
                    item =>
                        item.UserId == userId)
                .OrderByDescending(
                    item =>
                        item.LastUsedAtUtc)
                .ToArray();

        return Task.FromResult(
            result);
    }

    public Task AddAsync(
        UserTrustedDevice device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            device);

        _items.Add(
            device);

        return Task.CompletedTask;
    }
}