namespace OFOQ.Market.Application.Identity.Mfa.CompleteEnrollment;

public sealed record CompleteMfaEnrollmentResult(
    IReadOnlyList<string> RecoveryCodes,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc);
