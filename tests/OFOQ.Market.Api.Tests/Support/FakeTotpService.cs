using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakeTotpService :
    ITotpService
{
    public const string ValidCode =
        "123456";

    public const long VerifiedTimeStep =
        101;

    public MfaEnrollmentData CreateEnrollment(
        string accountName,
        string issuer)
    {
        throw new NotSupportedException(
            "Enrollment is not used by these API tests.");
    }

    public TotpVerificationResult Verify(
        string secret,
        string code,
        DateTimeOffset nowUtc)
    {
        if (secret !=
            "TEST-RAW-SECRET")
        {
            return new TotpVerificationResult(
                false,
                null);
        }

        if (code !=
            ValidCode)
        {
            return new TotpVerificationResult(
                false,
                null);
        }

        return new TotpVerificationResult(
            true,
            VerifiedTimeStep);
    }
}