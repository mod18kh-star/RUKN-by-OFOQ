namespace OFOQ.Market.Application.Tenancy.StoreProfile;

public sealed record ReplaceStoreSocialLinkInput(
    string PlatformCode,
    string? Label,
    string Url,
    bool IsVisible);

public sealed record ReplaceStoreSocialLinksCommand(
    IReadOnlyList<ReplaceStoreSocialLinkInput> SocialLinks,
    Guid ActorUserId);
