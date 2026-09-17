using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Fulfillment;

public sealed class FulfillmentLocation :
    Entity<FulfillmentLocationId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxCodeLength = 60;
    public const int MaxNameLength = 160;
    public const int MaxPhoneLength = 40;
    public const int MaxCountryCodeLength = 2;
    public const int MaxAddressLength = 240;

    private FulfillmentLocation() { }

    private FulfillmentLocation(
        FulfillmentLocationId id,
        TenantId tenantId,
        string code,
        string name,
        string? phone,
        string countryCode,
        string city,
        string? region,
        string line1,
        string? line2,
        bool isDefault,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        TenantId = tenantId;
        Apply(code, name, phone, countryCode, city, region, line1, line2);
        IsDefault = isDefault;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string CountryCode { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public string Line1 { get; private set; } = string.Empty;
    public string? Line2 { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static FulfillmentLocation Create(TenantId tenantId, string code, string name, string? phone, string countryCode, string city, string? region, string line1, string? line2, bool isDefault, DateTimeOffset at, Guid? by = null)
        => new(FulfillmentLocationId.New(), tenantId, code, name, phone, countryCode, city, region, line1, line2, isDefault, at, by);

    public void Update(string code, string name, string? phone, string countryCode, string city, string? region, string line1, string? line2, bool isActive, DateTimeOffset at, Guid? by = null)
    {
        Apply(code, name, phone, countryCode, city, region, line1, line2);
        IsActive = isActive;
        if (!isActive) IsDefault = false;
        UpdatedAtUtc = at;
        UpdatedByUserId = by;
    }

    public void SetDefault(bool value, DateTimeOffset at, Guid? by = null)
    {
        if (value && !IsActive) throw new InvalidOperationException("An inactive fulfillment location cannot be default.");
        IsDefault = value;
        UpdatedAtUtc = at;
        UpdatedByUserId = by;
    }

    private void Apply(string code, string name, string? phone, string countryCode, string city, string? region, string line1, string? line2)
    {
        Code = Required(code, MaxCodeLength, "Location code").ToLowerInvariant();
        Name = Required(name, MaxNameLength, "Location name");
        Phone = Optional(phone, MaxPhoneLength, "Phone");
        CountryCode = Required(countryCode, MaxCountryCodeLength, "Country code").ToUpperInvariant();
        if (CountryCode.Length != 2) throw new ArgumentException("Country code must be an ISO 3166-1 alpha-2 code.");
        City = Required(city, MaxNameLength, "City");
        Region = Optional(region, MaxNameLength, "Region");
        Line1 = Required(line1, MaxAddressLength, "Address line 1");
        Line2 = Optional(line2, MaxAddressLength, "Address line 2");
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
