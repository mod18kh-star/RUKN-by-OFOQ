namespace OFOQ.Market.Application.Identity.EmailVerification.Start;

public sealed record StartEmailVerificationResult(
    string Email,
    DateTimeOffset ExpiresAtUtc,
    string VerificationToken);
