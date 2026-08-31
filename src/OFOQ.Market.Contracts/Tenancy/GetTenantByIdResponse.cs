namespace OFOQ.Market.Contracts.Tenancy;

public sealed record GetTenantByIdResponse(
    Guid TenantId,
    string Name,
    string Slug,
    string Status,
    DateTimeOffset CreatedAtUtc);