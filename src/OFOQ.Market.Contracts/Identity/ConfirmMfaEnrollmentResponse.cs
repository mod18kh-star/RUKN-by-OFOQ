namespace OFOQ.Market.Contracts.Identity;

public sealed record ConfirmMfaEnrollmentResponse(
    IReadOnlyList<string> RecoveryCodes,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc);
