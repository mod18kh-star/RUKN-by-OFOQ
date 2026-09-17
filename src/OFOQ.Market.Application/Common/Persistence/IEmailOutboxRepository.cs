using OFOQ.Market.Domain.Notifications;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IEmailOutboxRepository
{
    Task AddAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailOutboxMessageId>> GetDueIdsAsync(
        DateTimeOffset nowUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task<bool> TryClaimAsync(
        EmailOutboxMessageId id,
        DateTimeOffset nowUtc,
        DateTimeOffset leaseExpiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<EmailOutboxMessage?> GetByIdAsync(
        EmailOutboxMessageId id,
        CancellationToken cancellationToken = default);
}
