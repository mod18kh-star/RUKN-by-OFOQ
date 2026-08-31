namespace OFOQ.Market.Contracts.Tenancy;

public sealed record CreateTenantResponse(
    Guid TenantId,
    string Name,
    string Slug,
    string Status);