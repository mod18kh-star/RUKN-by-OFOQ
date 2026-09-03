using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class UserMfaRecoveryCode :
    Entity<UserMfaRecoveryCodeId>,
    IAuditable
{
    private UserMfaRecoveryCode(
        UserMfaRecoveryCodeId id,
        UserMfaId userMfaId,
        string codeHash,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        UserMfaId =
            userMfaId;

        CodeHash =
            codeHash;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    private UserMfaRecoveryCode()
    {
    }

    public UserMfaId UserMfaId { get; private set; }

    public string CodeHash { get; private set; } =
        string.Empty;

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsed =>
        UsedAtUtc.HasValue;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static UserMfaRecoveryCode Create(
        UserMfaId userMfaId,
        string codeHash,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (userMfaId.IsEmpty)
        {
            throw new ArgumentException(
                "User MFA ID cannot be empty.",
                nameof(userMfaId));
        }

        if (string.IsNullOrWhiteSpace(
                codeHash))
        {
            throw new ArgumentException(
                "Recovery code hash is required.",
                nameof(codeHash));
        }

        return new UserMfaRecoveryCode(
            UserMfaRecoveryCodeId.New(),
            userMfaId,
            codeHash,
            createdAtUtc,
            createdByUserId);
    }

    public void MarkUsed(
        DateTimeOffset usedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (IsUsed)
        {
            throw new InvalidOperationException(
                "Recovery code has already been used.");
        }

        UsedAtUtc =
            usedAtUtc;

        UpdatedAtUtc =
            usedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }
}