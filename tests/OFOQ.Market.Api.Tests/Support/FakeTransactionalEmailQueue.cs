using OFOQ.Market.Application.Common.Notifications;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakeTransactionalEmailQueue :
    ITransactionalEmailQueue
{
    private readonly object _syncRoot = new();

    private readonly List<QueuedEmailVerification>
        _emailVerifications = [];

    private readonly List<QueuedNewOrder>
        _newOrders = [];

    public IReadOnlyList<QueuedEmailVerification>
        EmailVerifications
    {
        get
        {
            lock (_syncRoot)
            {
                return _emailVerifications.ToArray();
            }
        }
    }

    public IReadOnlyList<QueuedNewOrder>
        NewOrders
    {
        get
        {
            lock (_syncRoot)
            {
                return _newOrders.ToArray();
            }
        }
    }

    public Task QueueEmailVerificationAsync(
        UserId userId,
        string email,
        string token,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _emailVerifications.Add(
                new QueuedEmailVerification(
                    userId,
                    email,
                    token,
                    expiresAtUtc,
                    createdAtUtc));
        }

        return Task.CompletedTask;
    }

    public Task QueueNewOrderAsync(
        TenantId tenantId,
        OrderId orderId,
        decimal totalAmount,
        string currencyCode,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _newOrders.Add(
                new QueuedNewOrder(
                    tenantId,
                    orderId,
                    totalAmount,
                    currencyCode,
                    createdAtUtc));
        }

        return Task.CompletedTask;
    }

    internal sealed record QueuedEmailVerification(
        UserId UserId,
        string Email,
        string Token,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset CreatedAtUtc);

    internal sealed record QueuedNewOrder(
        TenantId TenantId,
        OrderId OrderId,
        decimal TotalAmount,
        string CurrencyCode,
        DateTimeOffset CreatedAtUtc);
}
