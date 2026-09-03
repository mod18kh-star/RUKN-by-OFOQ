namespace OFOQ.Market.Domain.Identity;

public readonly record struct EmailAddress
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Email address is required.",
                nameof(value));

        var normalized =
            value.Trim().ToLowerInvariant();

        if (normalized.Length > 254)
            throw new ArgumentException(
                "Email address cannot exceed 254 characters.",
                nameof(value));

        if (normalized.Any(char.IsWhiteSpace))
            throw new ArgumentException(
                "Email address cannot contain whitespace.",
                nameof(value));

        var atIndex = normalized.IndexOf('@');

        if (atIndex <= 0 ||
            atIndex != normalized.LastIndexOf('@') ||
            atIndex == normalized.Length - 1)
        {
            throw new ArgumentException(
                "Email address is invalid.",
                nameof(value));
        }

        var localPart =
            normalized[..atIndex];

        var domainPart =
            normalized[(atIndex + 1)..];

        if (localPart.Length > 64)
            throw new ArgumentException(
                "Email local part cannot exceed 64 characters.",
                nameof(value));

        if (!domainPart.Contains('.'))
            throw new ArgumentException(
                "Email domain is invalid.",
                nameof(value));

        if (domainPart.StartsWith('.') ||
            domainPart.EndsWith('.') ||
            domainPart.Contains(".."))
        {
            throw new ArgumentException(
                "Email domain is invalid.",
                nameof(value));
        }

        return new EmailAddress(normalized);
    }

    public override string ToString()
        => Value;
}