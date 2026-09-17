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
    string? CommercialRegistrationNumber,
    bool CommercialRegistrationNotApplicable,
    IReadOnlyList<StoreSocialLinkResponse> SocialLinks);
