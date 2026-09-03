using System.Security.Cryptography;
using System.Text;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Infrastructure.Security;

public sealed class HmacRecoveryCodeService :
    IRecoveryCodeService
{
    private const string Alphabet =
        "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private const int RawCodeLength =
        20;

    private const int GroupSize =
        4;

    private readonly byte[] _key;

    public HmacRecoveryCodeService(
        string base64Key)
    {
        if (string.IsNullOrWhiteSpace(
                base64Key))
        {
            throw new ArgumentException(
                "Recovery code HMAC key is required.",
                nameof(base64Key));
        }

        try
        {
            _key =
                Convert.FromBase64String(
                    base64Key);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Recovery code HMAC key must be valid Base64.",
                exception);
        }

        if (_key.Length < 32)
        {
            throw new InvalidOperationException(
                "Recovery code HMAC key must contain at least 256 bits.");
        }
    }

    public IReadOnlyList<string> GenerateCodes(
        int count)
    {
        if (count is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                "Recovery code count must be between 1 and 20.");
        }

        var codes =
            new HashSet<string>(
                StringComparer.Ordinal);

        while (codes.Count < count)
        {
            codes.Add(
                GenerateCode());
        }

        return codes.ToArray();
    }

    public string Hash(
        string code)
    {
        if (!TryNormalize(
                code,
                out var normalized))
        {
            throw new ArgumentException(
                "Recovery code format is invalid.",
                nameof(code));
        }

        using var hmac =
            new HMACSHA256(
                _key);

        var bytes =
            Encoding.UTF8.GetBytes(
                normalized);

        var hash =
            hmac.ComputeHash(
                bytes);

        return Convert.ToBase64String(
            hash);
    }

    public bool Verify(
        string code,
        string codeHash)
    {
        if (!TryNormalize(
                code,
                out var normalized)
            || string.IsNullOrWhiteSpace(
                codeHash))
        {
            return false;
        }

        byte[] expectedHash;

        try
        {
            expectedHash =
                Convert.FromBase64String(
                    codeHash);
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac =
            new HMACSHA256(
                _key);

        var actualHash =
            hmac.ComputeHash(
                Encoding.UTF8.GetBytes(
                    normalized));

        return CryptographicOperations
            .FixedTimeEquals(
                actualHash,
                expectedHash);
    }

    private static string GenerateCode()
    {
        Span<char> raw =
            stackalloc char[RawCodeLength];

        for (var index = 0;
             index < raw.Length;
             index++)
        {
            var alphabetIndex =
                RandomNumberGenerator.GetInt32(
                    Alphabet.Length);

            raw[index] =
                Alphabet[alphabetIndex];
        }

        var builder =
            new StringBuilder(
                RawCodeLength +
                (RawCodeLength / GroupSize) - 1);

        for (var index = 0;
             index < raw.Length;
             index++)
        {
            if (index > 0 &&
                index % GroupSize == 0)
            {
                builder.Append('-');
            }

            builder.Append(
                raw[index]);
        }

        return builder.ToString();
    }

    private static bool TryNormalize(
        string? code,
        out string normalized)
    {
        normalized =
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                code))
        {
            return false;
        }

        var builder =
            new StringBuilder(
                RawCodeLength);

        foreach (var character in code)
        {
            if (character == '-' ||
                char.IsWhiteSpace(
                    character))
            {
                continue;
            }

            var normalizedCharacter =
                char.ToUpperInvariant(
                    character);

            if (Alphabet.IndexOf(
                    normalizedCharacter) < 0)
            {
                return false;
            }

            builder.Append(
                normalizedCharacter);
        }

        if (builder.Length !=
            RawCodeLength)
        {
            return false;
        }

        normalized =
            builder.ToString();

        return true;
    }
}