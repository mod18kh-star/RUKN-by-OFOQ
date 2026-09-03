namespace OFOQ.Market.Contracts.Identity;

public sealed record VerifyMfaRecoveryCodeResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);