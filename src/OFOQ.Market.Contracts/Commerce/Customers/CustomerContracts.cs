namespace OFOQ.Market.Contracts.Commerce.Customers;

public sealed record UpdateCustomerProfileRequest(string? DisplayName, string? Phone);
public sealed record CustomerProfileResponse(Guid UserId, string? DisplayName, string? Phone, bool IsBlocked);
public sealed record CustomerAddressRequest(string Label, string RecipientName, string Phone, string CountryCode, string? Region, string City, string? PostalCode, string Line1, string? Line2, bool IsDefault, double? Latitude = null, double? Longitude = null, double? AccuracyMeters = null, string? MapUrl = null, string? DeliveryNotes = null);
public sealed record CustomerAddressResponse(Guid AddressId, string Label, string RecipientName, string Phone, string CountryCode, string? Region, string City, string? PostalCode, string Line1, string? Line2, bool IsDefault, bool IsActive, double? Latitude = null, double? Longitude = null, double? AccuracyMeters = null, string? MapUrl = null, string? DeliveryNotes = null);
