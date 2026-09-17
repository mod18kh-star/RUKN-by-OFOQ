namespace OFOQ.Market.Contracts.Commerce.Fulfillment;

public sealed record FulfillmentLocationRequest(string Code, string Name, string? Phone, string CountryCode, string City, string? Region, string Line1, string? Line2, bool IsActive, bool IsDefault);
public sealed record FulfillmentLocationResponse(Guid Id, string Code, string Name, string? Phone, string CountryCode, string City, string? Region, string Line1, string? Line2, bool IsDefault, bool IsActive);
public sealed record ShippingMethodRequest(string Code, string Name, string Type, decimal Price, string Currency, decimal? MinimumOrderAmount, decimal? MaximumOrderAmount, Guid? PickupLocationId, int SortOrder, bool IsEnabled);
public sealed record ShippingMethodResponse(Guid Id, string Code, string Name, string Type, decimal Price, string Currency, decimal? MinimumOrderAmount, decimal? MaximumOrderAmount, Guid? PickupLocationId, int SortOrder, bool IsEnabled);
