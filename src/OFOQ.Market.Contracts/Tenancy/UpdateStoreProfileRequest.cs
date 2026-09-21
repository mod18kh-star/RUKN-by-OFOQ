namespace OFOQ.Market.Contracts.Tenancy;

public sealed record UpdateStoreProfileRequest(
    string? WebsiteUrl,
    string? WhatsAppNumber,
    string? CustomerServicePhone,
    string? SecondaryPhone,
    string? LandlinePhone,
    string? PhysicalAddress,
    string? GoogleMapsUrl,
    string? CommercialRegistrationNumber,
    bool CommercialRegistrationNotApplicable,
    bool ShowWebsite,
    bool ShowWhatsApp,
    bool ShowCustomerServicePhone,
    bool ShowSecondaryPhone,
    bool ShowLandlinePhone,
    bool ShowPhysicalAddress,
    bool ShowCommercialRegistration);
