namespace OFOQ.Market.Contracts.Tenancy;

public sealed record StoreSocialLinkResponse(
    Guid SocialLinkId,
    string PlatformCode,
    string? Label,
    string Url,
    int SortOrder,
    bool IsVisible);

public sealed record StoreProfileResponse(
    Guid TenantId,
    string Name,
    string Slug,
    string Status,
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
    IReadOnlyList<StoreSocialLinkResponse> SocialLinks);
