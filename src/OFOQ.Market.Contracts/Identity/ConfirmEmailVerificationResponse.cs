namespace OFOQ.Market.Contracts.Identity;

public sealed record ConfirmEmailVerificationResponse(
    Guid UserId,
    string Email,
    DateTimeOffset VerifiedAtUtc);
