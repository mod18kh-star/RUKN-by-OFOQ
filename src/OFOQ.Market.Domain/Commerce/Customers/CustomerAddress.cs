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
        bool isDefault,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (userId.IsEmpty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        TenantId = tenantId;
        UserId = userId;
        Apply(label, recipientName, phone, countryCode, region, city, postalCode, line1, line2);
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
        Guid? createdByUserId = null)
        => new(CustomerAddressId.New(), tenantId, userId, label, recipientName, phone, countryCode, region, city, postalCode, line1, line2, isDefault, createdAtUtc, createdByUserId);

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
        Guid? updatedByUserId = null)
    {
        Apply(label, recipientName, phone, countryCode, region, city, postalCode, line1, line2);
        MarkUpdated(updatedAtUtc, updatedByUserId);
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

    private void Apply(string label, string recipientName, string phone, string countryCode, string? region, string city, string? postalCode, string line1, string? line2)
    {
        Label = Required(label, MaxLabelLength, "Address label");
        RecipientName = Required(recipientName, MaxNameLength, "Recipient name");
        Phone = Required(phone, MaxPhoneLength, "Phone");
        CountryCode = Required(countryCode, MaxCountryCodeLength, "Country code").ToUpperInvariant();
        if (CountryCode.Length != 2) throw new ArgumentException("Country code must be an ISO 3166-1 alpha-2 code.");
        Region = Optional(region, MaxRegionLength, "Region");
        City = Required(city, MaxCityLength, "City");
        PostalCode = Optional(postalCode, MaxPostalCodeLength, "Postal code");
        Line1 = Required(line1, MaxAddressLineLength, "Address line 1");
        Line2 = Optional(line2, MaxAddressLineLength, "Address line 2");
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
