namespace OFOQ.Market.Contracts.Identity;

public sealed record GoogleSignInRequest(
    string IdToken,
    string Nonce);
