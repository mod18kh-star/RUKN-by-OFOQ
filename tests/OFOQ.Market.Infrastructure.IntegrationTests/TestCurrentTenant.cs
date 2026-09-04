using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests;

internal sealed class TestCurrentTenant :
    ICurrentTenant
{
    public TestCurrentTenant(
        TenantId tenantId)
    {
        TenantId =
            tenantId;
    }

    public TenantId? TenantId { get; }

    public bool IsAvailable =>
        TenantId.HasValue &&
        !TenantId.Value.IsEmpty;
}