using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Commerce.Configuration.Common;

internal static class CommerceConfigurationRules
{
    public static TenantId GetRequiredTenantId(
        ICurrentTenant currentTenant)
    {
        ArgumentNullException.ThrowIfNull(
            currentTenant);

        if (!currentTenant.TenantId.HasValue ||
            currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "An active tenant context is required.");
        }

        return currentTenant.TenantId.Value;
    }

    public static void EnsureActor(
        Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(actorUserId));
        }
    }

    public static void EnsureVerticalType(
        CommerceVerticalType verticalType)
    {
        if (verticalType == CommerceVerticalType.Unknown ||
            !Enum.IsDefined(
                verticalType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(verticalType),
                "A supported commerce vertical is required.");
        }

        CommerceVerticalCatalog.Get(
            verticalType);
    }

    public static void EnsureCapabilityType(
        CommerceCapabilityType capabilityType)
    {
        if (capabilityType == CommerceCapabilityType.Unknown ||
            !Enum.IsDefined(
                capabilityType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(capabilityType),
                "A supported commerce capability is required.");
        }
    }

    public static CommerceProfileResult MapProfile(
        IReadOnlyCollection<TenantCommerceVertical> verticals,
        IReadOnlyCollection<TenantCommerceCapabilityOverride> overrides)
    {
        ArgumentNullException.ThrowIfNull(
            verticals);

        ArgumentNullException.ThrowIfNull(
            overrides);

        var effectiveCapabilities =
            CommerceCapabilityResolver.Resolve(
                verticals,
                overrides)
            .ToHashSet();

        var overriddenCapabilities =
            overrides
                .Select(
                    item =>
                        item.CapabilityType)
                .ToHashSet();

        var verticalResults =
            verticals
                .OrderByDescending(
                    vertical =>
                        vertical.IsPrimary)
                .ThenBy(
                    vertical =>
                        vertical.VerticalType)
                .Select(
                    vertical =>
                    {
                        var definition =
                            CommerceVerticalCatalog.Get(
                                vertical.VerticalType);

                        return new CommerceVerticalResult(
                            vertical.VerticalType,
                            definition.Code,
                            vertical.IsEnabled,
                            vertical.IsPrimary);
                    })
                .ToArray();

        var capabilityResults =
            Enum.GetValues<CommerceCapabilityType>()
                .Where(
                    capability =>
                        capability !=
                        CommerceCapabilityType.Unknown)
                .OrderBy(
                    capability =>
                        capability)
                .Select(
                    capability =>
                        new CommerceCapabilityResult(
                            capability,
                            effectiveCapabilities.Contains(
                                capability),
                            overriddenCapabilities.Contains(
                                capability)))
                .ToArray();

        return new CommerceProfileResult(
            verticalResults,
            capabilityResults);
    }
}