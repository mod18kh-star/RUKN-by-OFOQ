namespace OFOQ.Market.Contracts.Identity;

public sealed record VerifyMfaRecoveryCodeRequest(
    string ChallengeToken,
    string RecoveryCode,
    bool RememberDevice = false);
