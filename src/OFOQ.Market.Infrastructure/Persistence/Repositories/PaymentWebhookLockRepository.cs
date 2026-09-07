using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class PaymentWebhookLockRepository :
    IPaymentWebhookLockRepository
{
    private readonly MarketDbContext _dbContext;

    public PaymentWebhookLockRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AcquireAsync(
        TenantPaymentMethodId tenantPaymentMethodId,
        string externalEventId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        if (tenantPaymentMethodId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant payment method ID cannot be empty.",
                nameof(tenantPaymentMethodId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(externalEventId);

        var normalizedEventId = externalEventId.Trim();

        if (normalizedEventId.Length > 200)
        {
            throw new ArgumentException(
                "External event ID cannot exceed 200 characters.",
                nameof(externalEventId));
        }

        var lockKey = string.Concat(
            tenantId.Value.ToString("N"),
            ":",
            tenantPaymentMethodId.Value.ToString("N"),
            ":",
            normalizedEventId);

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    private OFOQ.Market.Domain.Tenancy.TenantId GetRequiredTenantId()
    {
        if (!_dbContext.HasCurrentTenant ||
            _dbContext.CurrentTenantId.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required for payment webhook locking.");
        }

        return _dbContext.CurrentTenantId;
    }
}
