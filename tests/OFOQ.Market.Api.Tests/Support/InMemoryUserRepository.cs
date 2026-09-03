using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryUserRepository :
    IUserRepository
{
    private readonly List<User> _users = [];

    public Task<User?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var user =
            _users.FirstOrDefault(
                item =>
                    item.Id == userId);

        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(
        EmailAddress email,
        CancellationToken cancellationToken = default)
    {
        var user =
            _users.FirstOrDefault(
                item =>
                    item.Email == email);

        return Task.FromResult(user);
    }

    public Task<bool> EmailExistsAsync(
        EmailAddress email,
        UserId? excludingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var exists =
            _users.Any(
                user =>
                    user.Email == email &&
                    (!excludingUserId.HasValue ||
                     user.Id != excludingUserId.Value));

        return Task.FromResult(exists);
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        _users.Add(user);

        return Task.CompletedTask;
    }
}