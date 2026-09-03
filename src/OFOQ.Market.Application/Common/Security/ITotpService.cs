namespace OFOQ.Market.Application.Common.Security;

public interface ITotpService
{
    MfaEnrollmentData CreateEnrollment(
        string accountName,
        string issuer);

    TotpVerificationResult Verify(
        string secret,
        string code,
        DateTimeOffset nowUtc);
}