namespace OFOQ.Market.Contracts.Identity;

public sealed record RefreshSessionResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
