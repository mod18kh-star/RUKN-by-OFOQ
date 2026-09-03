namespace OFOQ.Market.Api.Security;

public sealed class JwtSettings
{
    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException(
                "JWT issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException(
                "JWT audience is required.");
        }

        if (AccessTokenMinutes is < 5 or > 60)
        {
            throw new InvalidOperationException(
                "JWT access token lifetime must be between 5 and 60 minutes.");
        }

        _ = GetSigningKeyBytes();
    }

    public byte[] GetSigningKeyBytes()
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException(
                "JWT signing key is required.");
        }

        byte[] bytes;

        try
        {
            bytes =
                Convert.FromBase64String(
                    SigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "JWT signing key must be valid Base64.",
                exception);
        }

        if (bytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 256 bits.");
        }

        return bytes;
    }
}