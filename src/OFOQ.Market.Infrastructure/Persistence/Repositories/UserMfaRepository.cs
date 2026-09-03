using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class UserMfaRepository :
    IUserMfaRepository
{
    private readonly MarketDbContext _dbContext;

    public UserMfaRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserMfa?> GetByIdAsync(
        UserMfaId userMfaId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserMfas
            .SingleOrDefaultAsync(
                userMfa =>
                    userMfa.Id == userMfaId,
                cancellationToken);
    }

    public Task<UserMfa?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserMfas
            .SingleOrDefaultAsync(
                userMfa =>
                    userMfa.UserId == userId,
                cancellationToken);
    }

    public Task<bool> ExistsForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserMfas
            .AnyAsync(
                userMfa =>
                    userMfa.UserId == userId,
                cancellationToken);
    }

    public Task AddAsync(
        UserMfa userMfa,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            userMfa);

        return _dbContext.UserMfas
            .AddAsync(
                userMfa,
                cancellationToken)
            .AsTask();
    }
}