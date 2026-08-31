namespace OFOQ.Market.Domain.Tenancy;

public readonly record struct TenantSlug
{
    private static readonly HashSet<string> ReservedSlugs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "www",
            "api",
            "admin",
            "administrator",
            "app",
            "apps",
            "market",
            "support",
            "help",
            "mail",
            "email",
            "cdn",
            "static",
            "assets",
            "media",
            "images",
            "files",
            "auth",
            "login",
            "account",
            "accounts",
            "dashboard",
            "system",
            "status",
            "billing",
            "payments",
            "checkout",
            "store",
            "stores",
            "shop",
            "shops",
            "ofoq"
        };

    private TenantSlug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static TenantSlug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Tenant slug is required.",
                nameof(value));
        }

        value = value.Trim().ToLowerInvariant();

        if (value.Length is < 2 or > 63)
        {
            throw new ArgumentException(
                "Tenant slug must be between 2 and 63 characters.",
                nameof(value));
        }

        if (!IsAsciiLetterOrDigit(value[0]) ||
            !IsAsciiLetterOrDigit(value[^1]))
        {
            throw new ArgumentException(
                "Tenant slug must start and end with a letter or number.",
                nameof(value));
        }

        foreach (var character in value)
        {
            if (!IsAsciiLetterOrDigit(character) &&
                character != '-')
            {
                throw new ArgumentException(
                    "Tenant slug can contain only lowercase English letters, numbers, and hyphens.",
                    nameof(value));
            }
        }

        if (value.StartsWith("xn--", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Tenant slug cannot use the reserved IDN prefix.",
                nameof(value));
        }

        if (ReservedSlugs.Contains(value))
        {
            throw new ArgumentException(
                "Tenant slug is reserved by OFOQ.Market.",
                nameof(value));
        }

        return new TenantSlug(value);
    }

    public override string ToString() => Value;

    private static bool IsAsciiLetterOrDigit(char character)
        => character is >= 'a' and <= 'z'
           || character is >= '0' and <= '9';
}