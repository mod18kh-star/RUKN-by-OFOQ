using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Notifications;

public interface ITransactionalEmailQueue
{
    Task QueueEmailVerificationAsync(
        UserId userId,
        string email,
        string token,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default);

    Task QueueNewOrderAsync(
        TenantId tenantId,
        OrderId orderId,
        decimal totalAmount,
        string currencyCode,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default);
}
