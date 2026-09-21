namespace OFOQ.Market.Application.Tenancy.StoreProfile;

public sealed record UpdateStoreProfileCommand(
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
    bool ShowCommercialRegistration,
    Guid ActorUserId);
