namespace OFOQ.Market.Contracts.Identity;

public sealed record RegisterUserResponse(
    Guid UserId,
    string Email,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string? FullName = null,
    string? PhoneNumber = null);
