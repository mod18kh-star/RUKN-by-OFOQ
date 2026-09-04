using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Tenancy;

public interface ICurrentTenant
{
    TenantId? TenantId { get; }

    bool IsAvailable =>
        TenantId.HasValue &&
        !TenantId.Value.IsEmpty;
}