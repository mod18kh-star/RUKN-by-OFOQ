using OFOQ.Market.Application.Common.Security;
using OtpNet;

namespace OFOQ.Market.Infrastructure.Security;

internal sealed class OtpNetTotpService :
    ITotpService
{
    private const int SecretLengthBytes = 32;
    private const int TotpStepSeconds = 30;
    private const int TotpDigits = 6;

    public MfaEnrollmentData CreateEnrollment(
        string accountName,
        string issuer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            accountName);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            issuer);

        var secretBytes =
            KeyGeneration.GenerateRandomKey(
                SecretLengthBytes);

        var secret =
            Base32Encoding.ToString(
                secretBytes);

        var encodedLabel =
            Uri.EscapeDataString(
                $"{issuer}:{accountName}");

        var encodedIssuer =
            Uri.EscapeDataString(
                issuer);

        var provisioningUri =
            $"otpauth://totp/{encodedLabel}" +
            $"?secret={secret}" +
            $"&issuer={encodedIssuer}" +
            "&algorithm=SHA1" +
            $"&digits={TotpDigits}" +
            $"&period={TotpStepSeconds}";

        return new MfaEnrollmentData(
            secret,
            provisioningUri);
    }

    public TotpVerificationResult Verify(
        string secret,
        string code,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            secret);

        if (!IsValidCodeFormat(
                code))
        {
            return new TotpVerificationResult(
                false,
                null);
        }

        byte[] secretBytes;

        try
        {
            secretBytes =
                Base32Encoding.ToBytes(
                    secret);
        }
        catch (Exception exception)
            when (exception is ArgumentException
                  or FormatException)
        {
            throw new InvalidOperationException(
                "The stored TOTP secret is invalid.",
                exception);
        }

        var totp =
            new Totp(
                secretBytes,
                step: TotpStepSeconds,
                mode: OtpHashMode.Sha1,
                totpSize: TotpDigits);

        var isValid =
            totp.VerifyTotp(
                nowUtc.UtcDateTime,
                code,
                out var timeStepMatched,
                new VerificationWindow(
                    previous: 1,
                    future: 1));

        if (!isValid)
        {
            return new TotpVerificationResult(
                false,
                null);
        }

        return new TotpVerificationResult(
            true,
            timeStepMatched);
    }

    private static bool IsValidCodeFormat(
        string code)
    {
        if (string.IsNullOrWhiteSpace(
                code)
            || code.Length != TotpDigits)
        {
            return false;
        }

        foreach (var character in code)
        {
            if (character < '0' ||
                character > '9')
            {
                return false;
            }
        }

        return true;
    }
}