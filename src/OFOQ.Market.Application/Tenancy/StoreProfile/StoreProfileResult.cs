namespace OFOQ.Market.Application.Tenancy.StoreProfile;

public sealed record StoreProfileResult(
    Guid TenantId,
    string Name,
    string Slug,
    string Status,
    string? WebsiteUrl,
    string? WhatsAppNumber,
    string? CustomerServicePhone,
    string? CommercialRegistrationNumber,
    bool CommercialRegistrationNotApplicable,
    IReadOnlyList<StoreSocialLinkResult> SocialLinks);
