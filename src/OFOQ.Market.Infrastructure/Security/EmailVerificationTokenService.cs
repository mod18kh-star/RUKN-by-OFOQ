using System.Security.Cryptography;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Infrastructure.Security;

internal sealed class EmailVerificationTokenService :
    IEmailVerificationTokenService
{
    private const int TokenByteLength =
        32;

    private const int TokenHexLength =
        TokenByteLength * 2;

    public EmailVerificationToken Create()
    {
        var tokenBytes =
            RandomNumberGenerator.GetBytes(
                TokenByteLength);

        try
        {
            var token =
                Convert.ToHexString(
                    tokenBytes);

            return new EmailVerificationToken(
                token,
                ComputeHash(
                    tokenBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                tokenBytes);
        }
    }

    public bool TryHash(
        string? token,
        out string tokenHash)
    {
        tokenHash =
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                token) ||
            token.Length !=
                TokenHexLength)
        {
            return false;
        }

        byte[] tokenBytes;

        try
        {
            tokenBytes =
                Convert.FromHexString(
                    token);
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            if (tokenBytes.Length !=
                TokenByteLength)
            {
                return false;
            }

            tokenHash =
                ComputeHash(
                    tokenBytes);

            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                tokenBytes);
        }
    }

    private static string ComputeHash(
        ReadOnlySpan<byte> tokenBytes)
    {
        Span<byte> hashBytes =
            stackalloc byte[
                SHA256.HashSizeInBytes];

        SHA256.HashData(
            tokenBytes,
            hashBytes);

        return Convert.ToHexString(
            hashBytes);
    }
}
