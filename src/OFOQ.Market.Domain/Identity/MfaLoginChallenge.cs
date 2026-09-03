using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class MfaLoginChallenge :
    Entity<MfaLoginChallengeId>,
    IAuditable
{
    public const int MaximumFailedAttempts =
        5;

    private MfaLoginChallenge(
        MfaLoginChallengeId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        UserId =
            userId;

        TokenHash =
            tokenHash;

        ExpiresAtUtc =
            expiresAtUtc;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    private MfaLoginChallenge()
    {
    }

    public UserId UserId { get; private set; }

    public string TokenHash { get; private set; } =
        string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public int FailedAttemptCount { get; private set; }

    public bool IsConsumed =>
        ConsumedAtUtc.HasValue;

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsExhausted =>
        FailedAttemptCount >=
        MaximumFailedAttempts;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static MfaLoginChallenge Create(
        UserId userId,
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

        if (string.IsNullOrWhiteSpace(
                tokenHash))
        {
            throw new ArgumentException(
                "MFA challenge token hash is required.",
                nameof(tokenHash));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "MFA challenge expiration must be after creation time.",
                nameof(expiresAtUtc));
        }

        return new MfaLoginChallenge(
            MfaLoginChallengeId.New(),
            userId,
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
            !IsExhausted &&
            nowUtc < ExpiresAtUtc;
    }

    public void RegisterFailedAttempt(
        DateTimeOffset nowUtc,
        Guid? updatedByUserId = null)
    {
        EnsureUsable(
            nowUtc);

        FailedAttemptCount++;

        MarkUpdated(
            nowUtc,
            updatedByUserId);
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
            throw new InvalidOperationException(
                "A consumed MFA login challenge cannot be revoked.");
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
                "The MFA login challenge is no longer usable.");
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
}