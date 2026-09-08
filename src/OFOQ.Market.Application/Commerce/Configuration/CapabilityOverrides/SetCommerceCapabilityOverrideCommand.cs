using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Commerce.Configuration.CapabilityOverrides;

public sealed record SetCommerceCapabilityOverrideCommand(
    CommerceCapabilityType CapabilityType,
    bool? IsEnabled,
    Guid ActorUserId);