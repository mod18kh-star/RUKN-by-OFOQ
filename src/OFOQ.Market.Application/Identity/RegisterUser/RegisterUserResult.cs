using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.RegisterUser;

public sealed record RegisterUserResult(
    UserId UserId,
    string Email,
    UserStatus Status,
    DateTimeOffset CreatedAtUtc,
    string? FullName = null,
    string? PhoneNumber = null);
