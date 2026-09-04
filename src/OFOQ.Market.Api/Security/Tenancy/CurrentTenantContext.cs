using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Security.Tenancy;

public sealed class CurrentTenantContext :
    ICurrentTenant
{
    public TenantId? TenantId { get; private set; }

    public bool IsAvailable =>
        TenantId.HasValue &&
        !TenantId.Value.IsEmpty;

    public void SetTenant(
        TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        /*
         * A request may never silently switch from one tenant
         * to another after its scope has already been resolved.
         */
        if (TenantId.HasValue &&
            TenantId.Value != tenantId)
        {
            throw new InvalidOperationException(
                "The current request tenant scope has already been established.");
        }

        TenantId =
            tenantId;
    }
}