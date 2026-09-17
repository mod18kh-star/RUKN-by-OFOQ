namespace OFOQ.Market.Application.Tenancy.StoreProfile;

public sealed record UpdateStoreProfileCommand(
    string? WebsiteUrl,
    string? WhatsAppNumber,
    string? CustomerServicePhone,
    string? CommercialRegistrationNumber,
    bool CommercialRegistrationNotApplicable,
    Guid ActorUserId);
