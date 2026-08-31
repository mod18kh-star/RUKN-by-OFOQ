using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.GetTenantById;

public sealed record GetTenantByIdResult(
    TenantId TenantId,
    string Name,
    string Slug,
    TenantStatus Status,
    DateTimeOffset CreatedAtUtc);