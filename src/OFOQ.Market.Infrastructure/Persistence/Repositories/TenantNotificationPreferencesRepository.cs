using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Notifications;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class TenantNotificationPreferencesRepository :
    ITenantNotificationPreferencesRepository
{
    private readonly MarketDbContext _dbContext;

    public TenantNotificationPreferencesRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantNotificationPreferences?> GetAsync(
        CancellationToken cancellationToken = default)
        => _dbContext
            .TenantNotificationPreferences
            .SingleOrDefaultAsync(
                cancellationToken);

    public Task AddAsync(
        TenantNotificationPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        return _dbContext
            .TenantNotificationPreferences
            .AddAsync(
                preferences,
                cancellationToken)
            .AsTask();
    }
}
