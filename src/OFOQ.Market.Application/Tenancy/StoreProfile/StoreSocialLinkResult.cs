namespace OFOQ.Market.Application.Tenancy.StoreProfile;

public sealed record StoreSocialLinkResult(
    Guid SocialLinkId,
    string PlatformCode,
    string? Label,
    string Url,
    int SortOrder,
    bool IsVisible);
