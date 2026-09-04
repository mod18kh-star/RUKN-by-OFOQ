using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Common;

/// <summary>
/// Marks business data that must always be isolated
/// to exactly one tenant.
///
/// This marker is intentionally NOT used by control-plane
/// entities such as TenantDomain and TenantMembership.
/// </summary>
public interface ITenantDataScoped :
    ITenantScoped<TenantId>
{
}