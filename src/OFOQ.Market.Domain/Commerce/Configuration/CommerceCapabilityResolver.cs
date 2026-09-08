namespace OFOQ.Market.Domain.Commerce.Configuration;

public static class CommerceCapabilityResolver
{
    public static IReadOnlyCollection<CommerceCapabilityType> Resolve(
        IEnumerable<TenantCommerceVertical> verticals,
        IEnumerable<TenantCommerceCapabilityOverride> overrides)
    {
        ArgumentNullException.ThrowIfNull(
            verticals);

        ArgumentNullException.ThrowIfNull(
            overrides);

        var enabledVerticals =
            verticals
                .Where(
                    vertical =>
                        vertical.IsEnabled)
                .ToArray();

        var effectiveCapabilities =
            new HashSet<CommerceCapabilityType>();

        foreach (var vertical in enabledVerticals)
        {
            var definition =
                CommerceVerticalCatalog.Get(
                    vertical.VerticalType);

            foreach (var capability in definition.DefaultCapabilities)
            {
                effectiveCapabilities.Add(
                    capability);
            }
        }

        foreach (var capabilityOverride in overrides)
        {
            if (capabilityOverride.IsEnabled)
            {
                effectiveCapabilities.Add(
                    capabilityOverride.CapabilityType);
            }
            else
            {
                effectiveCapabilities.Remove(
                    capabilityOverride.CapabilityType);
            }
        }

        return effectiveCapabilities
            .OrderBy(
                capability =>
                    capability)
            .ToArray();
    }

    public static bool HasCapability(
        IEnumerable<TenantCommerceVertical> verticals,
        IEnumerable<TenantCommerceCapabilityOverride> overrides,
        CommerceCapabilityType capabilityType)
    {
        if (capabilityType == CommerceCapabilityType.Unknown)
        {
            return false;
        }

        return Resolve(
                verticals,
                overrides)
            .Contains(
                capabilityType);
    }
}