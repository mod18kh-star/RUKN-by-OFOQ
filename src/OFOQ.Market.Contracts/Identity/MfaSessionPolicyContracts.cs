namespace OFOQ.Market.Contracts.Identity;

public sealed record VerifyMfaReauthenticationRequest(
    string Code);

public sealed record UpdateMfaReopenPolicyRequest(
    string Policy);

public sealed record UpdateMfaReopenPolicyResponse(
    string Policy);