using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class UserMfa :
    AggregateRoot<UserMfaId>,
    IAuditable
{
    private UserMfa(
        UserMfaId id,
        UserId userId,
        string protectedSecret,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        UserId = userId;
        ProtectedSecret = protectedSecret;
        Status = UserMfaStatus.PendingEnrollment;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    private UserMfa()
    {
    }

    public UserId UserId { get; private set; }

    public string ProtectedSecret { get; private set; } =
        string.Empty;

    public UserMfaStatus Status { get; private set; }

    public DateTimeOffset? EnabledAtUtc { get; private set; }

    public long? LastAcceptedTimeStep { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static UserMfa BeginEnrollment(
        UserId userId,
        string protectedSecret,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (userId.IsEmpty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        ValidateProtectedSecret(
            protectedSecret);

        return new UserMfa(
            UserMfaId.New(),
            userId,
            protectedSecret,
            createdAtUtc,
            createdByUserId);
    }

    public void RestartEnrollment(
        string protectedSecret,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == UserMfaStatus.Enabled)
        {
            throw new InvalidOperationException(
                "Enabled MFA cannot be restarted through enrollment.");
        }

        ValidateProtectedSecret(
            protectedSecret);

        ProtectedSecret =
            protectedSecret;

        EnabledAtUtc =
            null;

        LastAcceptedTimeStep =
            null;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ConfirmEnrollment(
        long verifiedTimeStep,
        DateTimeOffset enabledAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == UserMfaStatus.Enabled)
        {
            throw new InvalidOperationException(
                "MFA is already enabled.");
        }

        if (verifiedTimeStep < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(verifiedTimeStep));
        }

        Status =
            UserMfaStatus.Enabled;

        EnabledAtUtc =
            enabledAtUtc;

        LastAcceptedTimeStep =
            verifiedTimeStep;

        MarkUpdated(
            enabledAtUtc,
            updatedByUserId);
    }

    public void AcceptTimeStep(
        long timeStep,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status != UserMfaStatus.Enabled)
        {
            throw new InvalidOperationException(
                "MFA must be enabled before accepting TOTP codes.");
        }

        if (timeStep < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeStep));
        }

        if (LastAcceptedTimeStep.HasValue &&
            timeStep <= LastAcceptedTimeStep.Value)
        {
            throw new InvalidOperationException(
                "This TOTP time step has already been used.");
        }

        LastAcceptedTimeStep =
            timeStep;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    private static void ValidateProtectedSecret(
        string protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(
                protectedSecret))
        {
            throw new ArgumentException(
                "Protected MFA secret is required.",
                nameof(protectedSecret));
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