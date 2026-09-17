namespace OFOQ.Market.Contracts.Identity;

public sealed record StartEmailVerificationResponse(
    string Email,
    DateTimeOffset ExpiresAtUtc,
    string? DevelopmentVerificationToken);
