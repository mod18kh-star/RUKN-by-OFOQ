namespace OFOQ.Market.Contracts.Tenancy;

public sealed record UpdateStoreProfileRequest(
    string? WebsiteUrl,
    string? WhatsAppNumber,
    string? CustomerServicePhone,
    string? CommercialRegistrationNumber,
    bool CommercialRegistrationNotApplicable);
