using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryPaymentIntentStore
{
    public object SyncRoot { get; } = new();
    public List<InMemoryPaymentIntentEntry> Items { get; } = [];
}

internal sealed record InMemoryPaymentIntentEntry(
    PaymentIntent Intent,
    string CreateIdempotencyKey);

internal sealed class InMemoryPaymentIntentRepository :
    IPaymentIntentRepository
{
    private readonly InMemoryPaymentIntentStore _store;
    private readonly ICurrentTenant _currentTenant;

    public InMemoryPaymentIntentRepository(
        InMemoryPaymentIntentStore store,
        ICurrentTenant currentTenant)
    {
        _store = store;
        _currentTenant = currentTenant;
    }

    public Task<PaymentIntent?> GetByIdAsync(
        PaymentIntentId paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                _store.Items
                    .Select(item => item.Intent)
                    .SingleOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.Id == paymentIntentId));
        }
    }

    public Task<PaymentIntent?> GetByCreateIdempotencyKeyAsync(
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                _store.Items
                    .SingleOrDefault(item =>
                        item.Intent.TenantId == tenantId &&
                        item.Intent.CustomerUserId == customerUserId &&
                        string.Equals(
                            item.CreateIdempotencyKey,
                            idempotencyKey,
                            StringComparison.Ordinal))
                    ?.Intent);
        }
    }

    public Task<PaymentIntent?> GetByProviderReferenceAsync(
        TenantPaymentMethodId tenantPaymentMethodId,
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                _store.Items
                    .Select(item => item.Intent)
                    .SingleOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.TenantPaymentMethodId == tenantPaymentMethodId &&
                        item.ProviderReference == providerReference));
        }
    }

    public Task AddAsync(
        PaymentIntent paymentIntent,
        string createIdempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(createIdempotencyKey);

        var tenantId = GetRequiredTenantId();

        if (paymentIntent.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant payment intent creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            if (_store.Items.Any(item =>
                    item.Intent.TenantId == tenantId &&
                    item.Intent.CustomerUserId == paymentIntent.CustomerUserId &&
                    string.Equals(
                        item.CreateIdempotencyKey,
                        createIdempotencyKey,
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "The payment idempotency key is already in use.");
            }

            _store.Items.Add(
                new InMemoryPaymentIntentEntry(
                    paymentIntent,
                    createIdempotencyKey));
        }

        return Task.CompletedTask;
    }

    private TenantId GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}
