namespace OFOQ.Market.Contracts.Identity;

public sealed record RegenerateMfaRecoveryCodesResponse(
    IReadOnlyList<string> RecoveryCodes);
