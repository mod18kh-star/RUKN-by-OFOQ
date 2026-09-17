using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class UserTrustedDeviceRepository :
    IUserTrustedDeviceRepository
{
    private readonly MarketDbContext _dbContext;

    public UserTrustedDeviceRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserTrustedDevice?> GetByIdAsync(
        UserTrustedDeviceId id,
        CancellationToken cancellationToken = default)
        => _dbContext
            .UserTrustedDevices
            .SingleOrDefaultAsync(
                item =>
                    item.Id == id,
                cancellationToken);

    public async Task<IReadOnlyList<UserTrustedDevice>>
        GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        => await _dbContext
            .UserTrustedDevices
            .Where(
                item =>
                    item.UserId == userId)
            .OrderByDescending(
                item =>
                    item.LastUsedAtUtc)
            .ToListAsync(
                cancellationToken);

    public Task AddAsync(
        UserTrustedDevice device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        return _dbContext
            .UserTrustedDevices
            .AddAsync(
                device,
                cancellationToken)
            .AsTask();
    }
}
