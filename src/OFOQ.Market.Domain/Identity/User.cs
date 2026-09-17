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
        Guid? createdByUserId,
        string? fullName,
        string? phoneNumber)
        : base(id)
    {
        Email = email;
        PasswordHash = NormalizePasswordHash(passwordHash);
        FullName = NormalizeOptionalFullName(fullName);
        PhoneNumber = NormalizeOptionalPhoneNumber(phoneNumber);
        Status = UserStatus.Active;

        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    private User()
    {
    }

    public EmailAddress Email { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public string? FullName { get; private set; }

    public string? PhoneNumber { get; private set; }

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
        Guid? createdByUserId = null,
        string? fullName = null,
        string? phoneNumber = null)
    {
        var user =
            new User(
                UserId.New(),
                EmailAddress.Create(email),
                passwordHash,
                createdAtUtc,
                createdByUserId,
                fullName,
                phoneNumber);

        user.RaiseDomainEvent(
            new UserRegisteredDomainEvent(
                user.Id,
                user.Email,
                createdAtUtc));

        return user;
    }

    public void UpdateProfile(
        string? fullName,
        string? phoneNumber,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        FullName =
            NormalizeOptionalFullName(
                fullName);

        PhoneNumber =
            NormalizeOptionalPhoneNumber(
                phoneNumber);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
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

    public void ChangeEmail(
        string email,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var normalized = EmailAddress.Create(email);

        if (Email == normalized)
        {
            return;
        }

        Email = normalized;
        EmailVerifiedAtUtc = null;

        MarkUpdated(
            updatedAtUtc,
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

    private static string? NormalizeOptionalFullName(
        string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var normalized =
            fullName.Trim();

        if (normalized.Length is < 2 or > 160)
        {
            throw new ArgumentException(
                "Full name must be between 2 and 160 characters.",
                nameof(fullName));
        }

        return normalized;
    }

    private static string? NormalizeOptionalPhoneNumber(
        string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var normalized =
            new string(
                phoneNumber
                    .Trim()
                    .Where(
                        character =>
                            !char.IsWhiteSpace(character) &&
                            character is not '-' and not '(' and not ')')
                    .ToArray());

        var digits =
            normalized.StartsWith(
                '+')
                ? normalized[1..]
                : normalized;

        if (digits.Length is < 8 or > 20 ||
            digits.Any(
                character =>
                    !char.IsDigit(character)))
        {
            throw new ArgumentException(
                "Phone number must contain between 8 and 20 digits and may start with +.",
                nameof(phoneNumber));
        }

        return normalized;
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