using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.CreateTenant;

public sealed record CreateTenantResult(
    TenantId TenantId,
    string Name,
    string Slug,
    TenantStatus Status);