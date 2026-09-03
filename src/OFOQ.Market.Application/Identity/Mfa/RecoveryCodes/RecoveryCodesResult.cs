namespace OFOQ.Market.Application.Identity.Mfa.RecoveryCodes;

public sealed record RecoveryCodesResult(
    IReadOnlyList<string> Codes);