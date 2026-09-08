namespace OFOQ.Market.Contracts.Commerce.Configuration;

public sealed record SetCommerceCapabilityOverrideRequest(
    string CapabilityType,
    bool? Enabled);