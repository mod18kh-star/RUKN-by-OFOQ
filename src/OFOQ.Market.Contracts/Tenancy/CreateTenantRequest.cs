namespace OFOQ.Market.Contracts.Tenancy;

public sealed record CreateTenantRequest(
    string Name,
    string Slug);