using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Tenancy;

public sealed class TenantStoreProfile :
    Entity<TenantStoreProfileId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxWebsiteUrlLength = 2048;
    public const int MaxPhoneLength = 40;
    public const int MaxCommercialRegistrationLength = 120;
    public const int MaxPhysicalAddressLength = 500;
    public const int MaxGoogleMapsUrlLength = 2048;

    private TenantStoreProfile()
    {
    }

    private TenantStoreProfile(
        TenantStoreProfileId id,
        TenantId tenantId,
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? secondaryPhone,
        string? landlinePhone,
        string? physicalAddress,
        string? googleMapsUrl,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable,
        bool showWebsite,
        bool showWhatsApp,
        bool showCustomerServicePhone,
        bool showSecondaryPhone,
        bool showLandlinePhone,
        bool showPhysicalAddress,
        bool showCommercialRegistration,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        TenantId = tenantId;

        Apply(
            websiteUrl,
            whatsAppNumber,
            customerServicePhone,
            secondaryPhone,
            landlinePhone,
            physicalAddress,
            googleMapsUrl,
            commercialRegistrationNumber,
            commercialRegistrationNotApplicable,
            showWebsite,
            showWhatsApp,
            showCustomerServicePhone,
            showSecondaryPhone,
            showLandlinePhone,
            showPhysicalAddress,
            showCommercialRegistration);

        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public string? WebsiteUrl { get; private set; }

    public string? WhatsAppNumber { get; private set; }

    public string? CustomerServicePhone { get; private set; }

    public string? SecondaryPhone { get; private set; }

    public string? LandlinePhone { get; private set; }

    public string? PhysicalAddress { get; private set; }

    public string? GoogleMapsUrl { get; private set; }

    public string? CommercialRegistrationNumber { get; private set; }

    public bool CommercialRegistrationNotApplicable { get; private set; }

    public bool ShowWebsite { get; private set; }

    public bool ShowWhatsApp { get; private set; }

    public bool ShowCustomerServicePhone { get; private set; }

    public bool ShowSecondaryPhone { get; private set; }

    public bool ShowLandlinePhone { get; private set; }

    public bool ShowPhysicalAddress { get; private set; }

    public bool ShowCommercialRegistration { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantStoreProfile Create(
        TenantId tenantId,
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? secondaryPhone,
        string? landlinePhone,
        string? physicalAddress,
        string? googleMapsUrl,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable,
        bool showWebsite,
        bool showWhatsApp,
        bool showCustomerServicePhone,
        bool showSecondaryPhone,
        bool showLandlinePhone,
        bool showPhysicalAddress,
        bool showCommercialRegistration,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantStoreProfile(
            TenantStoreProfileId.New(),
            tenantId,
            websiteUrl,
            whatsAppNumber,
            customerServicePhone,
            secondaryPhone,
            landlinePhone,
            physicalAddress,
            googleMapsUrl,
            commercialRegistrationNumber,
            commercialRegistrationNotApplicable,
            showWebsite,
            showWhatsApp,
            showCustomerServicePhone,
            showSecondaryPhone,
            showLandlinePhone,
            showPhysicalAddress,
            showCommercialRegistration,
            createdAtUtc,
            createdByUserId);
    }

    public static TenantStoreProfile Create(
        TenantId tenantId,
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return Create(
            tenantId,
            websiteUrl,
            whatsAppNumber,
            customerServicePhone,
            null,
            null,
            null,
            null,
            commercialRegistrationNumber,
            commercialRegistrationNotApplicable,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            createdAtUtc,
            createdByUserId);
    }

    public void Update(
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? secondaryPhone,
        string? landlinePhone,
        string? physicalAddress,
        string? googleMapsUrl,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable,
        bool showWebsite,
        bool showWhatsApp,
        bool showCustomerServicePhone,
        bool showSecondaryPhone,
        bool showLandlinePhone,
        bool showPhysicalAddress,
        bool showCommercialRegistration,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        Apply(
            websiteUrl,
            whatsAppNumber,
            customerServicePhone,
            secondaryPhone,
            landlinePhone,
            physicalAddress,
            googleMapsUrl,
            commercialRegistrationNumber,
            commercialRegistrationNotApplicable,
            showWebsite,
            showWhatsApp,
            showCustomerServicePhone,
            showSecondaryPhone,
            showLandlinePhone,
            showPhysicalAddress,
            showCommercialRegistration);

        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private void Apply(
        string? websiteUrl,
        string? whatsAppNumber,
        string? customerServicePhone,
        string? secondaryPhone,
        string? landlinePhone,
        string? physicalAddress,
        string? googleMapsUrl,
        string? commercialRegistrationNumber,
        bool commercialRegistrationNotApplicable,
        bool showWebsite,
        bool showWhatsApp,
        bool showCustomerServicePhone,
        bool showSecondaryPhone,
        bool showLandlinePhone,
        bool showPhysicalAddress,
        bool showCommercialRegistration)
    {
        var normalizedCommercialRegistrationNumber =
            NormalizeOptional(
                commercialRegistrationNumber,
                MaxCommercialRegistrationLength,
                "Commercial registration number");

        if (commercialRegistrationNotApplicable &&
            normalizedCommercialRegistrationNumber is not null)
        {
            throw new ArgumentException(
                "Commercial registration number cannot be provided when commercial registration is marked as not applicable.",
                nameof(commercialRegistrationNumber));
        }

        WebsiteUrl = NormalizeHttpUrl(
            websiteUrl,
            MaxWebsiteUrlLength,
            "Website URL");

        WhatsAppNumber = NormalizeOptional(
            whatsAppNumber,
            MaxPhoneLength,
            "WhatsApp number");

        CustomerServicePhone = NormalizeOptional(
            customerServicePhone,
            MaxPhoneLength,
            "Customer service phone");

        SecondaryPhone = NormalizeOptional(
            secondaryPhone,
            MaxPhoneLength,
            "Secondary phone");

        LandlinePhone = NormalizeOptional(
            landlinePhone,
            MaxPhoneLength,
            "Landline phone");

        PhysicalAddress = NormalizeOptional(
            physicalAddress,
            MaxPhysicalAddressLength,
            "Physical address");

        GoogleMapsUrl = NormalizeHttpUrl(
            googleMapsUrl,
            MaxGoogleMapsUrlLength,
            "Google Maps URL");

        CommercialRegistrationNumber = normalizedCommercialRegistrationNumber;
        CommercialRegistrationNotApplicable = commercialRegistrationNotApplicable;

        ShowWebsite = showWebsite;
        ShowWhatsApp = showWhatsApp;
        ShowCustomerServicePhone = showCustomerServicePhone;
        ShowSecondaryPhone = showSecondaryPhone;
        ShowLandlinePhone = showLandlinePhone;
        ShowPhysicalAddress = showPhysicalAddress;
        ShowCommercialRegistration =
            !commercialRegistrationNotApplicable &&
            showCommercialRegistration;
    }

    private static string? NormalizeHttpUrl(
        string? value,
        int maxLength,
        string fieldName)
    {
        var normalized =
            NormalizeOptional(
                value,
                maxLength,
                fieldName);

        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException(
                $"{fieldName} must be an absolute HTTP or HTTPS URL.",
                nameof(value));
        }

        return uri.AbsoluteUri;
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
