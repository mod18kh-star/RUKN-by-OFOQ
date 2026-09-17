using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakeTotpService :
    ITotpService
{
    public const string ValidCode =
        "123456";

    public const long VerifiedTimeStep =
        101;

    public const string EnrollmentSecret =
        "TEST-RAW-SECRET";

    public MfaEnrollmentData CreateEnrollment(
        string accountName,
        string issuer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            accountName);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            issuer);

        return new MfaEnrollmentData(
            EnrollmentSecret,
            $"otpauth://totp/{issuer}:{accountName}?secret={EnrollmentSecret}&issuer={issuer}");
    }

    public TotpVerificationResult Verify(
        string secret,
        string code,
        DateTimeOffset nowUtc)
    {
        if (secret !=
            EnrollmentSecret)
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
