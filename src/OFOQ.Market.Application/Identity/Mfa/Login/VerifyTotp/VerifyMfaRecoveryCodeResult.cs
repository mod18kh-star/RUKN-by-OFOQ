using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.Login.VerifyRecovery;

public sealed record VerifyMfaRecoveryCodeResult(
    UserId UserId,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);