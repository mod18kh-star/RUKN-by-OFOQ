using System.Security.Cryptography;
using System.Text;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Security;

public sealed class RefreshTokenService :
    IRefreshTokenService
{
    private const string Version = "v1";
    private const int SecretByteCount = 32;

    public RefreshTokenMaterial Create(
        UserSessionId sessionId)
    {
        if (sessionId.IsEmpty)
        {
            throw new ArgumentException(
                "User session ID cannot be empty.",
                nameof(sessionId));
        }

        var secret =
            ToBase64Url(
                RandomNumberGenerator.GetBytes(
                    SecretByteCount));

        var token =
            $"{Version}.{sessionId.Value:N}.{secret}";

        return new RefreshTokenMaterial(
            token,
            Hash(token));
    }

    public bool TryHash(
        string? token,
        out UserSessionId sessionId,
        out string tokenHash)
    {
        sessionId = default;
        tokenHash = string.Empty;

        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 256)
        {
            return false;
        }

        var parts = token.Split('.');

        if (parts.Length != 3 ||
            !string.Equals(
                parts[0],
                Version,
                StringComparison.Ordinal) ||
            parts[1].Length != 32 ||
            parts[2].Length < 40 ||
            !Guid.TryParseExact(
                parts[1],
                "N",
                out var sessionGuid) ||
            sessionGuid == Guid.Empty)
        {
            return false;
        }

        sessionId =
            UserSessionId.From(
                sessionGuid);

        tokenHash = Hash(token);

        return true;
    }

    public bool FixedTimeEquals(
        string leftHash,
        string rightHash)
    {
        if (string.IsNullOrWhiteSpace(leftHash) ||
            string.IsNullOrWhiteSpace(rightHash))
        {
            return false;
        }

        byte[] left;
        byte[] right;

        try
        {
            left = Convert.FromHexString(leftHash);
            right = Convert.FromHexString(rightHash);
        }
        catch (FormatException)
        {
            return false;
        }

        return left.Length == right.Length &&
            CryptographicOperations.FixedTimeEquals(
                left,
                right);
    }

    private static string Hash(
        string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    token)));
    }

    private static string ToBase64Url(
        byte[] bytes)
    {
        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
