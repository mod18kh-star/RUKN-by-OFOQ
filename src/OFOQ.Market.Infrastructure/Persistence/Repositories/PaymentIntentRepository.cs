using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class PaymentIntentRepository :
    IPaymentIntentRepository
{
    private readonly MarketDbContext _dbContext;

    public PaymentIntentRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PaymentIntent?> GetByIdAsync(
        PaymentIntentId paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentIntents
            .Include("_transactions")
            .SingleOrDefaultAsync(
                intent => intent.Id == paymentIntentId,
                cancellationToken);
    }

    public Task<PaymentIntent?> GetByCreateIdempotencyKeyAsync(
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (customerUserId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return _dbContext.PaymentIntents
            .Include("_transactions")
            .SingleOrDefaultAsync(
                intent =>
                    EF.Property<Guid>(intent, "_customerUserId") == customerUserId.Value &&
                    EF.Property<string?>(intent, "CreateIdempotencyKey") == idempotencyKey,
                cancellationToken);
    }

    public Task<PaymentIntent?> GetByProviderReferenceAsync(
        TenantPaymentMethodId tenantPaymentMethodId,
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        if (tenantPaymentMethodId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant payment method ID cannot be empty.",
                nameof(tenantPaymentMethodId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(providerReference);

        return _dbContext.PaymentIntents
            .Include("_transactions")
            .SingleOrDefaultAsync(
                intent =>
                    intent.TenantPaymentMethodId == tenantPaymentMethodId &&
                    intent.ProviderReference == providerReference,
                cancellationToken);
    }

    public Task AddAsync(
        PaymentIntent paymentIntent,
        string createIdempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(createIdempotencyKey);

        var normalizedKey = createIdempotencyKey.Trim();

        if (normalizedKey.Length > 128)
        {
            throw new ArgumentException(
                "Idempotency key cannot exceed 128 characters.",
                nameof(createIdempotencyKey));
        }

        var entry = _dbContext.PaymentIntents.Add(paymentIntent);

        entry.Property<string?>("CreateIdempotencyKey")
            .CurrentValue = normalizedKey;

        return Task.CompletedTask;
    }
}
