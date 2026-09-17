using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.ProductRelations;

public sealed class ProductRecommendationSettingsHandler
{
    private readonly ITenantProductRecommendationSettingsRepository _repository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ProductRecommendationSettingsHandler(
        ITenantProductRecommendationSettingsRepository repository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ProductRecommendationSettingsResult> GetAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureTenant();

        var settings =
            await _repository.GetAsync(
                cancellationToken);

        return new ProductRecommendationSettingsResult(
            settings?.IsEnabled ?? true,
            settings?.AutomaticSuggestionsEnabled ?? false);
    }

    public async Task<ProductRecommendationSettingsResult> UpdateAsync(
        UpdateProductRecommendationSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureTenant();

        var settings =
            await _repository.GetAsync(
                cancellationToken);

        var now = _timeProvider.GetUtcNow();

        if (settings is null)
        {
            settings =
                TenantProductRecommendationSettings.Create(
                    _currentTenant.TenantId!.Value,
                    command.IsEnabled,
                    command.AutomaticSuggestionsEnabled,
                    now,
                    command.ActorUserId.Value);

            await _repository.AddAsync(
                settings,
                cancellationToken);
        }
        else
        {
            settings.Update(
                command.IsEnabled,
                command.AutomaticSuggestionsEnabled,
                now,
                command.ActorUserId.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductRecommendationSettingsResult(
            settings.IsEnabled,
            settings.AutomaticSuggestionsEnabled);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to manage recommendation settings.");
        }
    }
}
