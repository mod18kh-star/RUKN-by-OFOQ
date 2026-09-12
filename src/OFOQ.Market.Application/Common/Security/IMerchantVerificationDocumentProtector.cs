using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Security;

public sealed record ProtectedMerchantDocumentNumber(
    string ProtectedValue,
    string Fingerprint);

public interface IMerchantVerificationDocumentProtector
{
    ProtectedMerchantDocumentNumber ProtectDocumentNumber(
        TenantId tenantId,
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string documentNumber);

    string UnprotectDocumentNumber(
        TenantId tenantId,
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string protectedDocumentNumber);

    string ComputeDocumentNumberFingerprint(
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string documentNumber);
}