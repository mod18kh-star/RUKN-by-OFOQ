namespace OFOQ.Market.Domain.Commerce.Verification;

public enum MerchantVerificationDocumentType
{
    Unknown = 0,

    NationalId = 10,

    Passport = 20,

    ResidencePermit = 30,

    CommercialRegistration = 40,

    TradeLicense = 50,

    TaxRegistration = 60,

    OtherGovernmentId = 70,

    OtherBusinessDocument = 80
}