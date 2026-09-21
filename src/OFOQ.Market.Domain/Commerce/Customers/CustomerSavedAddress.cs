using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Commerce.Customers;

public sealed record SavedAddressDetails(
    string Label,
    string RecipientName,
    string Phone,
    string CountryCode,
    string? Region,
    string City,
    string? PostalCode,
    string Line1,
    string? Line2,
    decimal? Latitude,
    decimal? Longitude,
    string? MapUrl,
    string? DeliveryNotes
);

public sealed class CustomerSavedAddress
{
    private CustomerSavedAddress() { }

    public Guid Id { get; private set; }

    public UserId UserId { get; private set; }

    public string Label { get; private set; } = string.Empty;

    public string RecipientName { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string CountryCode { get; private set; } = string.Empty;

    public string? Region { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string? PostalCode { get; private set; }

    public string Line1 { get; private set; } = string.Empty;

    public string? Line2 { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public string? MapUrl { get; private set; }

    public string? DeliveryNotes { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static CustomerSavedAddress Create(
        UserId userId,
        SavedAddressDetails details,
        DateTimeOffset now)
    {
        if (userId.IsEmpty)
            throw new ArgumentException("User ID is required.");

        var address = new CustomerSavedAddress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsActive = true,
            CreatedAtUtc = now
        };

        address.Apply(details);

        return address;
    }

    public void Update(
        SavedAddressDetails details,
        DateTimeOffset now)
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "Inactive address cannot be updated.");

        Apply(details);

        UpdatedAtUtc = now;
    }

    public void MakeDefault(DateTimeOffset now)
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "Inactive address cannot be the default.");

        IsDefault = true;
        UpdatedAtUtc = now;
    }

    public void ClearDefault(DateTimeOffset now)
    {
        IsDefault = false;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        IsDefault = false;
        UpdatedAtUtc = now;
    }

    private void Apply(SavedAddressDetails details)
    {
        Label = Required(details.Label, 80);

        RecipientName = Required(details.RecipientName, 160);

        Phone = Required(details.Phone, 40);

        CountryCode = Required(
            details.CountryCode, 2).ToUpperInvariant();

        if (CountryCode.Length != 2 ||
            !CountryCode.All(c => c >= 'A' && c <= 'Z'))
        {
            throw new ArgumentException(
                "Invalid ISO country code.");
        }

        Region = Optional(details.Region, 120);

        City = Required(details.City, 120);

        PostalCode = Optional(details.PostalCode, 32);

        Line1 = Required(details.Line1, 240);

        Line2 = Optional(details.Line2, 240);

        DeliveryNotes = Optional(details.DeliveryNotes, 600);

        var mapUrl = Optional(details.MapUrl, 2048);

        if (mapUrl is not null)
        {
            if (!Uri.TryCreate(
                    mapUrl,
                    UriKind.Absolute,
                    out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(uri.UserInfo))
            {
                throw new ArgumentException(
                    "A valid HTTPS map URL is required.");
            }
        }

        if (details.Latitude.HasValue !=
            details.Longitude.HasValue)
        {
            throw new ArgumentException(
                "Latitude and longitude must be provided together.");
        }

        if (details.Latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(details.Latitude));
        }

        if (details.Longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(details.Longitude));
        }

        Latitude = details.Latitude;
        Longitude = details.Longitude;
        MapUrl = mapUrl;
    }

    private static string Required(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "A required address field is missing.");

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new ArgumentException(
                "Address field exceeds its maximum length.");

        return normalized;
    }

    private static string? Optional(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new ArgumentException(
                "Address field exceeds its maximum length.");

        return normalized;
    }
}