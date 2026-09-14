using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Commerce.Analytics;

public sealed class GetMerchantAnalyticsHandler
{
    private static readonly TimeSpan
        MaximumRange =
            TimeSpan.FromDays(
                366);

    private readonly IMerchantAnalyticsQueryRepository
        _repository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly TimeProvider
        _timeProvider;

    public GetMerchantAnalyticsHandler(
        IMerchantAnalyticsQueryRepository repository,
        ICurrentTenant currentTenant,
        TimeProvider timeProvider)
    {
        _repository =
            repository;

        _currentTenant =
            currentTenant;

        _timeProvider =
            timeProvider;
    }

    public Task<MerchantAnalyticsResult> HandleAsync(
        GetMerchantAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        EnsureTenant();

        if (query.TopProducts < 1 ||
            query.TopProducts > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.TopProducts),
                "Top products must be between 1 and 20.");
        }

        var toUtc =
            query.ToUtc
            ?? _timeProvider.GetUtcNow();

        var fromUtc =
            query.FromUtc
            ?? toUtc.AddDays(
                -30);

        if (fromUtc >=
            toUtc)
        {
            throw new ArgumentException(
                "Analytics start time must be earlier than the end time.");
        }

        if (toUtc -
            fromUtc >
            MaximumRange)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Analytics range cannot exceed 366 days.");
        }

        return _repository.GetAsync(
            fromUtc,
            toUtc,
            query.TopProducts,
            cancellationToken);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to query merchant analytics.");
        }
    }
}