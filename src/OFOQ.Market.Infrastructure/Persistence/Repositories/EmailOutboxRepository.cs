using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Notifications;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class EmailOutboxRepository :
    IEmailOutboxRepository
{
    private readonly MarketDbContext _dbContext;

    public EmailOutboxRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return _dbContext
            .EmailOutboxMessages
            .AddAsync(
                message,
                cancellationToken)
            .AsTask();
    }

    public async Task<IReadOnlyList<EmailOutboxMessageId>>
        GetDueIdsAsync(
            DateTimeOffset nowUtc,
            int take,
            CancellationToken cancellationToken = default)
    {
        var safeTake =
            Math.Clamp(
                take,
                1,
                100);

        return await _dbContext
            .EmailOutboxMessages
            .AsNoTracking()
            .Where(
                item =>
                    item.State != EmailOutboxState.Sent &&
                    item.NextAttemptAtUtc <= nowUtc &&
                    (
                        item.State != EmailOutboxState.Processing ||
                        item.LeaseExpiresAtUtc == null ||
                        item.LeaseExpiresAtUtc <= nowUtc
                    ))
            .OrderBy(
                item =>
                    item.NextAttemptAtUtc)
            .ThenBy(
                item =>
                    item.CreatedAtUtc)
            .Select(
                item =>
                    item.Id)
            .Take(
                safeTake)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<bool> TryClaimAsync(
        EmailOutboxMessageId id,
        DateTimeOffset nowUtc,
        DateTimeOffset leaseExpiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var affected =
            await _dbContext
                .EmailOutboxMessages
                .Where(
                    item =>
                        item.Id == id &&
                        item.State != EmailOutboxState.Sent &&
                        item.NextAttemptAtUtc <= nowUtc &&
                        (
                            item.State != EmailOutboxState.Processing ||
                            item.LeaseExpiresAtUtc == null ||
                            item.LeaseExpiresAtUtc <= nowUtc
                        ))
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                item =>
                                    item.State,
                                EmailOutboxState.Processing)
                            .SetProperty(
                                item =>
                                    item.LeaseExpiresAtUtc,
                                leaseExpiresAtUtc)
                            .SetProperty(
                                item =>
                                    item.Attempts,
                                item =>
                                    item.Attempts + 1),
                    cancellationToken);

        return affected == 1;
    }

    public Task<EmailOutboxMessage?> GetByIdAsync(
        EmailOutboxMessageId id,
        CancellationToken cancellationToken = default)
        => _dbContext
            .EmailOutboxMessages
            .SingleOrDefaultAsync(
                item =>
                    item.Id == id,
                cancellationToken);
}
