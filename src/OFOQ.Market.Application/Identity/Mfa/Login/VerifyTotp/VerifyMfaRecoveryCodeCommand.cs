namespace OFOQ.Market.Application.Identity.Mfa.Login.VerifyRecovery;

public sealed record VerifyMfaRecoveryCodeCommand(
    string ChallengeToken,
    string RecoveryCode);