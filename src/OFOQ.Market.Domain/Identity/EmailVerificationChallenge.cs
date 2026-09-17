using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class EmailVerificationChallenge :
    Entity<EmailVerificationChallengeId>,
    IAuditable
{
    private EmailVerificationChallenge(
        EmailVerificationChallengeId id,
        UserId userId,
        string emailSnapshot,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        UserId =
            userId;

        EmailSnapshot =
            EmailAddress.Create(
                emailSnapshot)
                .Value;

        TokenHash =
            NormalizeTokenHash(
                tokenHash);

        ExpiresAtUtc =
            expiresAtUtc;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    private EmailVerificationChallenge()
    {
    }

    public UserId UserId { get; private set; }

    public string EmailSnapshot { get; private set; } =
        string.Empty;

    public string TokenHash { get; private set; } =
        string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsConsumed =>
        ConsumedAtUtc.HasValue;

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public static EmailVerificationChallenge Create(
        UserId userId,
        string emailSnapshot,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (userId.IsEmpty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (expiresAtUtc <=
            createdAtUtc)
        {
            throw new ArgumentException(
                "Email verification expiration must be after creation time.",
                nameof(expiresAtUtc));
        }

        return new EmailVerificationChallenge(
            EmailVerificationChallengeId.New(),
            userId,
            emailSnapshot,
            tokenHash,
            expiresAtUtc,
            createdAtUtc,
            createdByUserId);
    }

    public bool IsUsable(
        DateTimeOffset nowUtc)
    {
        return
            !IsConsumed &&
            !IsRevoked &&
            nowUtc < ExpiresAtUtc;
    }

    public void Consume(
        DateTimeOffset nowUtc,
        Guid? updatedByUserId = null)
    {
        EnsureUsable(
            nowUtc);

        ConsumedAtUtc =
            nowUtc;

        MarkUpdated(
            nowUtc,
            updatedByUserId);
    }

    public void Revoke(
        DateTimeOffset nowUtc,
        Guid? updatedByUserId = null)
    {
        if (IsConsumed)
        {
            return;
        }

        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc =
            nowUtc;

        MarkUpdated(
            nowUtc,
            updatedByUserId);
    }

    private void EnsureUsable(
        DateTimeOffset nowUtc)
    {
        if (!IsUsable(
                nowUtc))
        {
            throw new InvalidOperationException(
                "The email verification challenge is no longer usable.");
        }
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private static string NormalizeTokenHash(
        string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(
                tokenHash))
        {
            throw new ArgumentException(
                "Email verification token hash is required.",
                nameof(tokenHash));
        }

        var normalized =
            tokenHash.Trim();

        if (normalized.Length >
            128)
        {
            throw new ArgumentException(
                "Email verification token hash is too long.",
                nameof(tokenHash));
        }

        return normalized;
    }
}
