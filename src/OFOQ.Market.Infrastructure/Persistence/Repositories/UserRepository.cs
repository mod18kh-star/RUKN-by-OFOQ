using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class UserRepository :
    IUserRepository
{
    private readonly MarketDbContext _dbContext;

    public UserRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        EmailAddress email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        EmailAddress email,
        UserId? excludingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var users =
            _dbContext.Users
                .IgnoreQueryFilters();

        if (excludingUserId.HasValue)
        {
            return users.AnyAsync(
                user =>
                    user.Email == email &&
                    user.Id != excludingUserId.Value,
                cancellationToken);
        }

        return users.AnyAsync(
            user => user.Email == email,
            cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _dbContext.Users.AddAsync(
            user,
            cancellationToken);
    }
}