namespace OFOQ.Market.Contracts.Identity;

public sealed record VerifyMfaTotpRequest(
    string ChallengeToken,
    string Code,
    bool RememberDevice = false);
