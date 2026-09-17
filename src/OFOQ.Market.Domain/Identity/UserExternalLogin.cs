using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class UserExternalLogin : Entity<UserExternalLoginId>
{
    public const string GoogleProvider = "google";

    private UserExternalLogin(
        UserExternalLoginId id,
        UserId userId,
        string provider,
        string subject,
        string emailAtLink,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        UserId = userId;
        Provider = NormalizeRequired(provider, 32, nameof(provider));
        Subject = NormalizeRequired(subject, 255, nameof(subject));
        EmailAtLink = EmailAddress.Create(emailAtLink).Value;
        CreatedAtUtc = createdAtUtc;
    }

    private UserExternalLogin() { }

    public UserId UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string EmailAtLink { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static UserExternalLogin Create(
        UserId userId,
        string provider,
        string subject,
        string emailAtLink,
        DateTimeOffset createdAtUtc)
    {
        if (userId.IsEmpty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return new UserExternalLogin(
            UserExternalLoginId.New(),
            userId,
            provider,
            subject,
            emailAtLink,
            createdAtUtc);
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);

        return normalized;
    }
}
