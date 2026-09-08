namespace OFOQ.Market.Contracts.Commerce.Configuration;

public sealed record CommerceVerticalResponse(
    string VerticalType,
    string Code,
    bool Enabled,
    bool Primary);

public sealed record CommerceCapabilityResponse(
    string CapabilityType,
    bool Enabled,
    bool Overridden);

public sealed record CommerceProfileResponse(
    IReadOnlyList<CommerceVerticalResponse> Verticals,
    IReadOnlyList<CommerceCapabilityResponse> Capabilities);