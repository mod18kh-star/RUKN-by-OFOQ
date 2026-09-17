namespace OFOQ.Market.Application.Identity.EmailVerification.Confirm;

public sealed record ConfirmEmailVerificationResult(
    Guid UserId,
    string Email,
    DateTimeOffset VerifiedAtUtc);
