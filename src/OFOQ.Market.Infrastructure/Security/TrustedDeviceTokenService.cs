using System.Security.Cryptography;
using System.Text;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Security;

public sealed class TrustedDeviceTokenService :
    ITrustedDeviceTokenService
{
    private const string Version = "v1";
    private const int SecretByteCount = 32;

    public TrustedDeviceTokenMaterial Create(
        UserTrustedDeviceId trustedDeviceId)
    {
        if (trustedDeviceId.IsEmpty)
            throw new ArgumentException(
                "Trusted device ID cannot be empty.",
                nameof(trustedDeviceId));

        var secret =
            ToBase64Url(
                RandomNumberGenerator.GetBytes(
                    SecretByteCount));

        var token =
            $"{Version}.{trustedDeviceId.Value:N}.{secret}";

        return new TrustedDeviceTokenMaterial(
            token,
            Hash(token));
    }

    public bool TryHash(
        string? token,
        out UserTrustedDeviceId trustedDeviceId,
        out string tokenHash)
    {
        trustedDeviceId = default;
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
                out var id) ||
            id == Guid.Empty)
        {
            return false;
        }

        trustedDeviceId =
            UserTrustedDeviceId.From(id);

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

        try
        {
            var left =
                Convert.FromHexString(leftHash);

            var right =
                Convert.FromHexString(rightHash);

            return left.Length == right.Length &&
                CryptographicOperations.FixedTimeEquals(
                    left,
                    right);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string HashUserAgent(
        string? userAgent)
    {
        var normalized =
            string.IsNullOrWhiteSpace(userAgent)
                ? string.Empty
                : userAgent.Trim();

        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    normalized)));
    }

    private static string Hash(
        string token)
        => Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    token)));

    private static string ToBase64Url(
        byte[] bytes)
        => Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
