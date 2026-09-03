using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class User :
    AggregateRoot<UserId>,
    IAuditable,
    ISoftDeletable
{
    private User(
        UserId id,
        EmailAddress email,
        string passwordHash,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        Email = email;
        PasswordHash = NormalizePasswordHash(passwordHash);
        Status = UserStatus.Active;

        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    private User()
    {
    }

    public EmailAddress Email { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public UserStatus Status { get; private set; }

    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static User Create(
        string email,
        string passwordHash,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        var user =
            new User(
                UserId.New(),
                EmailAddress.Create(email),
                passwordHash,
                createdAtUtc,
                createdByUserId);

        user.RaiseDomainEvent(
            new UserRegisteredDomainEvent(
                user.Id,
                user.Email,
                createdAtUtc));

        return user;
    }

    public void MarkEmailVerified(
        DateTimeOffset verifiedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (EmailVerifiedAtUtc.HasValue)
            return;

        EmailVerifiedAtUtc = verifiedAtUtc;

        MarkUpdated(
            verifiedAtUtc,
            updatedByUserId);
    }

    public void ChangePasswordHash(
        string passwordHash,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        PasswordHash =
            NormalizePasswordHash(passwordHash);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Suspend(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == UserStatus.Suspended)
            return;

        Status = UserStatus.Suspended;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Activate(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == UserStatus.Active)
            return;

        Status = UserStatus.Active;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Disable(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == UserStatus.Disabled)
            return;

        Status = UserStatus.Disabled;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Delete(
        DateTimeOffset deletedAtUtc,
        Guid? deletedByUserId = null)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;

        MarkUpdated(
            deletedAtUtc,
            deletedByUserId);
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static string NormalizePasswordHash(
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "Password hash is required.",
                nameof(passwordHash));

        var normalized =
            passwordHash.Trim();

        if (normalized.Length > 1024)
            throw new ArgumentException(
                "Password hash is too long.",
                nameof(passwordHash));

        return normalized;
    }
}