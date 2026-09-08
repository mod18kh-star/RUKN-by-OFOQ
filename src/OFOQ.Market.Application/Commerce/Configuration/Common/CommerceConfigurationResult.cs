using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Commerce.Configuration.Common;

public sealed record CommerceVerticalResult(
    CommerceVerticalType VerticalType,
    string Code,
    bool IsEnabled,
    bool IsPrimary);

public sealed record CommerceCapabilityResult(
    CommerceCapabilityType CapabilityType,
    bool IsEnabled,
    bool IsOverridden);

public sealed record CommerceProfileResult(
    IReadOnlyList<CommerceVerticalResult> Verticals,
    IReadOnlyList<CommerceCapabilityResult> Capabilities);