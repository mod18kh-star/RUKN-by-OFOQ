using System.Globalization;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Customers;

public sealed class CustomerAddress :
    Entity<CustomerAddressId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxLabelLength = 80;
    public const int MaxNameLength = 160;
    public const int MaxPhoneLength = 40;
    public const int MaxCountryCodeLength = 2;
    public const int MaxRegionLength = 120;
    public const int MaxCityLength = 120;
    public const int MaxPostalCodeLength = 32;
    public const int MaxAddressLineLength = 240;
    public const int MaxMapUrlLength = 2048;
    public const int MaxDeliveryNotesLength = 240;

    private CustomerAddress() { }

    private CustomerAddress(
        CustomerAddressId id,
        TenantId tenantId,
        UserId userId,
        string label,
        string recipientName,
        string phone,
        string countryCode,
        string? region,
        string city,
        string? postalCode,
        string line1,
        string? line2,
        double? latitude,
        double? longitude,
        double? accuracyMeters,
        string? mapUrl,
        string? deliveryNotes,
        bool isDefault,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (userId.IsEmpty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        TenantId = tenantId;
        UserId = userId;
        Apply(
            label,
            recipientName,
            phone,
            countryCode,
            region,
            city,
            postalCode,
            line1,
            line2,
            latitude,
            longitude,
            accuracyMeters,
            mapUrl,
            deliveryNotes);
        IsDefault = isDefault;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }
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
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double? AccuracyMeters { get; private set; }
    public string? MapUrl { get; private set; }
    public string? DeliveryNotes { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static CustomerAddress Create(
        TenantId tenantId,
        UserId userId,
        string label,
        string recipientName,
        string phone,
        string countryCode,
        string? region,
        string city,
        string? postalCode,
        string line1,
        string? line2,
        bool isDefault,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null,
        double? latitude = null,
        double? longitude = null,
        double? accuracyMeters = null,
        string? mapUrl = null,
        string? deliveryNotes = null)
        => new(
            CustomerAddressId.New(),
            tenantId,
            userId,
            label,
            recipientName,
            phone,
            countryCode,
            region,
            city,
            postalCode,
            line1,
            line2,
            latitude,
            longitude,
            accuracyMeters,
            mapUrl,
            deliveryNotes,
            isDefault,
            createdAtUtc,
            createdByUserId);

    public void Update(
        string label,
        string recipientName,
        string phone,
        string countryCode,
        string? region,
        string city,
        string? postalCode,
        string line1,
        string? line2,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null,
        double? latitude = null,
        double? longitude = null,
        double? accuracyMeters = null,
        string? mapUrl = null,
        string? deliveryNotes = null)
    {
        Apply(
            label,
            recipientName,
            phone,
            countryCode,
            region,
            city,
            postalCode,
            line1,
            line2,
            latitude,
            longitude,
            accuracyMeters,
            mapUrl,
            deliveryNotes);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void MakeDefault(DateTimeOffset updatedAtUtc, Guid? updatedByUserId = null)
    {
        if (!IsActive) throw new InvalidOperationException("An inactive address cannot be the default address.");
        IsDefault = true;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    public void ClearDefault(DateTimeOffset updatedAtUtc, Guid? updatedByUserId = null)
    {
        if (!IsDefault) return;
        IsDefault = false;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc, Guid? updatedByUserId = null)
    {
        IsActive = false;
        IsDefault = false;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    private void Apply(
        string label,
        string recipientName,
        string phone,
        string countryCode,
        string? region,
        string city,
        string? postalCode,
        string line1,
        string? line2,
        double? latitude,
        double? longitude,
        double? accuracyMeters,
        string? mapUrl,
        string? deliveryNotes)
    {
        Label =
            Required(
                label,
                MaxLabelLength,
                "Address label");

        RecipientName =
            Required(
                recipientName,
                MaxNameLength,
                "Recipient name");

        Phone =
            Required(
                phone,
                MaxPhoneLength,
                "Phone");

        ValidateCoordinates(
            latitude,
            longitude);

        if (
            accuracyMeters.HasValue &&
            !latitude.HasValue)
        {
            throw new ArgumentException(
                "Location accuracy requires coordinates.");
        }

        ValidateAccuracy(
            accuracyMeters);

        Latitude =
            latitude;

        Longitude =
            longitude;

        AccuracyMeters =
            accuracyMeters;

        MapUrl =
            latitude.HasValue &&
            longitude.HasValue
                ? CoordinateMapUrl(
                    latitude.Value,
                    longitude.Value)
                : NormalizeMapUrl(
                    mapUrl);

        var hasMapLocation =
            MapUrl is not null;

        if (
            string.IsNullOrWhiteSpace(
                countryCode
            )
        )
        {
            CountryCode =
                string.Empty;
        }
        else
        {
            CountryCode =
                Required(
                    countryCode,
                    MaxCountryCodeLength,
                    "Country code")
                .ToUpperInvariant();

            if (
                CountryCode.Length != 2
            )
            {
                throw new ArgumentException(
                    "Country code must be an ISO 3166-1 alpha-2 code.");
            }
        }

        Region =
            Optional(
                region,
                MaxRegionLength,
                "Region");

        City =
            string.IsNullOrWhiteSpace(
                city
            )
                ? string.Empty
                : Required(
                    city,
                    MaxCityLength,
                    "City");

        PostalCode =
            Optional(
                postalCode,
                MaxPostalCodeLength,
                "Postal code");

        var normalizedLine1 =
            Optional(
                line1,
                MaxAddressLineLength,
                "Address line 1");

        if (
            hasMapLocation &&
            CountryCode.Length == 0 &&
            City.Length == 0
        )
        {
            Line1 =
                MapUrl!.Length <=
                MaxAddressLineLength
                    ? MapUrl
                    : "موقع محدد على الخريطة";
        }
        else
        {
            Line1 =
                normalizedLine1 ??
                (
                    MapUrl is not null &&
                    MapUrl.Length <=
                    MaxAddressLineLength
                        ? MapUrl
                        : hasMapLocation
                            ? "موقع محدد على الخريطة"
                            : string.Empty
                );
        }

        Line2 =
            Optional(
                line2,
                MaxAddressLineLength,
                "Address line 2");

        DeliveryNotes =
            Optional(
                deliveryNotes,
                MaxDeliveryNotesLength,
                "Delivery notes");

        if (!hasMapLocation)
        {
            if (
                CountryCode.Length != 2 ||
                string.IsNullOrWhiteSpace(
                    City
                ) ||
                string.IsNullOrWhiteSpace(
                    Line1
                )
            )
            {
                throw new ArgumentException(
                    "A delivery address requires either a map location or the traditional country, city and address fields.");
            }
        }
    }

    private static void ValidateCoordinates(
        double? latitude,
        double? longitude)
    {
        if (
            latitude.HasValue !=
            longitude.HasValue
        )
        {
            throw new ArgumentException(
                "Latitude and longitude must be supplied together.");
        }

        if (!latitude.HasValue)
        {
            return;
        }

        if (
            double.IsNaN(
                latitude.Value
            ) ||
            double.IsInfinity(
                latitude.Value
            ) ||
            latitude.Value < -90d ||
            latitude.Value > 90d
        )
        {
            throw new ArgumentException(
                "Latitude is outside the valid range.");
        }

        if (
            double.IsNaN(
                longitude!.Value
            ) ||
            double.IsInfinity(
                longitude.Value
            ) ||
            longitude.Value < -180d ||
            longitude.Value > 180d
        )
        {
            throw new ArgumentException(
                "Longitude is outside the valid range.");
        }
    }

    private static void ValidateAccuracy(
        double? accuracyMeters)
    {
        if (!accuracyMeters.HasValue)
        {
            return;
        }

        if (
            double.IsNaN(
                accuracyMeters.Value
            ) ||
            double.IsInfinity(
                accuracyMeters.Value
            ) ||
            accuracyMeters.Value < 0d ||
            accuracyMeters.Value > 100000d
        )
        {
            throw new ArgumentException(
                "Location accuracy is outside the valid range.");
        }
    }

    private static string? NormalizeMapUrl(
        string? value)
    {
        if (
            string.IsNullOrWhiteSpace(
                value
            )
        )
        {
            return null;
        }

        var normalized =
            value.Trim();

        if (
            normalized.Length >
            MaxMapUrlLength
        )
        {
            throw new ArgumentException(
                $"Map URL cannot exceed {MaxMapUrlLength} characters.");
        }

        if (
            !Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri) ||
            !string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(
                uri.UserInfo
            )
        )
        {
            throw new ArgumentException(
                "Map URL must be a valid HTTPS URL.");
        }

        var host =
            uri.Host.ToLowerInvariant();

        var isGoogleMaps =
            host == "maps.app.goo.gl" ||
            host == "maps.google.com" ||
            (
                host == "goo.gl" &&
                uri.AbsolutePath.StartsWith(
                    "/maps",
                    StringComparison.OrdinalIgnoreCase)
            ) ||
            (
                (
                    host == "google.com" ||
                    host == "www.google.com" ||
                    host.EndsWith(
                        ".google.com",
                        StringComparison.Ordinal
                    )
                ) &&
                uri.AbsolutePath.StartsWith(
                    "/maps",
                    StringComparison.OrdinalIgnoreCase)
            );

        if (!isGoogleMaps)
        {
            throw new ArgumentException(
                "Map URL must be a Google Maps URL.");
        }

        return uri.AbsoluteUri;
    }

    private static string CoordinateMapUrl(
        double latitude,
        double longitude)
    {
        var lat =
            latitude.ToString(
                "0.######",
                CultureInfo.InvariantCulture);

        var lng =
            longitude.ToString(
                "0.######",
                CultureInfo.InvariantCulture);

        return
            $"https://www.google.com/maps/search/?api=1&query={lat},{lng}";
    }

    private void MarkUpdated(DateTimeOffset at, Guid? by)
    {
        UpdatedAtUtc = at;
        UpdatedByUserId = by;
    }

    private static string Required(string value, int max, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var n = value.Trim();
        if (n.Length > max) throw new ArgumentException($"{name} cannot exceed {max} characters.");
        return n;
    }

    private static string? Optional(string? value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var n = value.Trim();
        if (n.Length > max) throw new ArgumentException($"{name} cannot exceed {max} characters.");
        return n;
    }
}
