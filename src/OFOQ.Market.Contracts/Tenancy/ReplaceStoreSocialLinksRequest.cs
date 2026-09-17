namespace OFOQ.Market.Contracts.Tenancy;

public sealed record ReplaceStoreSocialLinkRequest(
    string PlatformCode,
    string? Label,
    string Url,
    bool IsVisible);

public sealed record ReplaceStoreSocialLinksRequest(
    IReadOnlyList<ReplaceStoreSocialLinkRequest> SocialLinks);
