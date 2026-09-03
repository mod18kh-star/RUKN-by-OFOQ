namespace OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;

public sealed record VerifyMfaTotpCommand(
    string ChallengeToken,
    string Code);