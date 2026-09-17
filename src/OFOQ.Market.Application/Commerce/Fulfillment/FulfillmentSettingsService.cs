using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Fulfillment;

namespace OFOQ.Market.Application.Commerce.Fulfillment;

public sealed record FulfillmentLocationResult(Guid Id, string Code, string Name, string? Phone, string CountryCode, string City, string? Region, string Line1, string? Line2, bool IsDefault, bool IsActive);
public sealed record ShippingMethodResult(Guid Id, string Code, string Name, string Type, decimal Price, string Currency, decimal? MinimumOrderAmount, decimal? MaximumOrderAmount, Guid? PickupLocationId, int SortOrder, bool IsEnabled);

public sealed class FulfillmentSettingsService
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IFulfillmentLocationRepository _locations;
    private readonly IShippingMethodRepository _shippingMethods;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public FulfillmentSettingsService(ICurrentTenant currentTenant, IFulfillmentLocationRepository locations, IShippingMethodRepository shippingMethods, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _currentTenant = currentTenant;
        _locations = locations;
        _shippingMethods = shippingMethods;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<FulfillmentLocationResult>> GetLocationsAsync(CancellationToken ct = default)
        => (await _locations.GetAllAsync(ct)).OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name).Select(Map).ToArray();

    public async Task<FulfillmentLocationResult> CreateLocationAsync(string code, string name, string? phone, string countryCode, string city, string? region, string line1, string? line2, bool isDefault, Guid actorUserId, CancellationToken ct = default)
    {
        EnsureTenant();
        var normalized = code.Trim().ToLowerInvariant();
        if (await _locations.CodeExistsAsync(normalized, cancellationToken: ct)) throw new ArgumentException("Fulfillment location code already exists.");
        var all = await _locations.GetAllAsync(ct);
        var now = _timeProvider.GetUtcNow();
        var makeDefault = isDefault || all.Count == 0;
        if (makeDefault) foreach (var existing in all.Where(x => x.IsDefault)) existing.SetDefault(false, now, actorUserId);
        var location = FulfillmentLocation.Create(_currentTenant.TenantId!.Value, code, name, phone, countryCode, city, region, line1, line2, makeDefault, now, actorUserId);
        await _locations.AddAsync(location, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(location);
    }

    public async Task<FulfillmentLocationResult?> UpdateLocationAsync(FulfillmentLocationId id, string code, string name, string? phone, string countryCode, string city, string? region, string line1, string? line2, bool isActive, bool isDefault, Guid actorUserId, CancellationToken ct = default)
    {
        EnsureTenant();
        var location = await _locations.GetByIdAsync(id, ct);
        if (location is null) return null;
        var normalized = code.Trim().ToLowerInvariant();
        if (await _locations.CodeExistsAsync(normalized, id, ct)) throw new ArgumentException("Fulfillment location code already exists.");
        var now = _timeProvider.GetUtcNow();
        location.Update(code, name, phone, countryCode, city, region, line1, line2, isActive, now, actorUserId);
        if (isDefault)
        {
            foreach (var existing in (await _locations.GetAllAsync(ct)).Where(x => x.Id != id && x.IsDefault)) existing.SetDefault(false, now, actorUserId);
            location.SetDefault(true, now, actorUserId);
        }
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(location);
    }

    public async Task<IReadOnlyList<ShippingMethodResult>> GetShippingMethodsAsync(bool onlyEnabled = false, CancellationToken ct = default)
    {
        var all = await _shippingMethods.GetAllAsync(ct);
        return all.Where(x => !onlyEnabled || x.IsEnabled).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(Map).ToArray();
    }

    public async Task<ShippingMethodResult> CreateShippingMethodAsync(string code, string name, ShippingMethodType type, decimal price, string currency, decimal? min, decimal? max, Guid? pickupLocationId, int sortOrder, Guid actorUserId, CancellationToken ct = default)
    {
        EnsureTenant();
        var normalized = code.Trim().ToLowerInvariant();
        if (await _shippingMethods.CodeExistsAsync(normalized, cancellationToken: ct)) throw new ArgumentException("Shipping method code already exists.");
        FulfillmentLocationId? locationId = pickupLocationId.HasValue ? FulfillmentLocationId.From(pickupLocationId.Value) : null;
        if (locationId.HasValue && await _locations.GetByIdAsync(locationId.Value, ct) is null) throw new ArgumentException("Pickup location was not found.");
        var method = ShippingMethod.Create(_currentTenant.TenantId!.Value, code, name, type, price, CurrencyCode.Create(currency), min, max, locationId, sortOrder, _timeProvider.GetUtcNow(), actorUserId);
        await _shippingMethods.AddAsync(method, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(method);
    }

    public async Task<ShippingMethodResult?> UpdateShippingMethodAsync(ShippingMethodId id, string code, string name, ShippingMethodType type, decimal price, string currency, decimal? min, decimal? max, Guid? pickupLocationId, int sortOrder, bool isEnabled, Guid actorUserId, CancellationToken ct = default)
    {
        EnsureTenant();
        var method = await _shippingMethods.GetByIdAsync(id, ct);
        if (method is null) return null;
        var normalized = code.Trim().ToLowerInvariant();
        if (await _shippingMethods.CodeExistsAsync(normalized, id, ct)) throw new ArgumentException("Shipping method code already exists.");
        FulfillmentLocationId? locationId = pickupLocationId.HasValue ? FulfillmentLocationId.From(pickupLocationId.Value) : null;
        if (locationId.HasValue && await _locations.GetByIdAsync(locationId.Value, ct) is null) throw new ArgumentException("Pickup location was not found.");
        method.Update(code, name, type, price, CurrencyCode.Create(currency), min, max, locationId, sortOrder, isEnabled, _timeProvider.GetUtcNow(), actorUserId);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(method);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable || !_currentTenant.TenantId.HasValue)
            throw new TenantScopeViolationException("Tenant context is required.");
    }

    private static FulfillmentLocationResult Map(FulfillmentLocation x) => new(x.Id.Value, x.Code, x.Name, x.Phone, x.CountryCode, x.City, x.Region, x.Line1, x.Line2, x.IsDefault, x.IsActive);
    private static ShippingMethodResult Map(ShippingMethod x) => new(x.Id.Value, x.Code, x.Name, x.Type.ToString(), x.Price, x.Currency.Value, x.MinimumOrderAmount, x.MaximumOrderAmount, x.PickupLocationId?.Value, x.SortOrder, x.IsEnabled);
}
