namespace OFOQ.Market.Contracts.Tenancy;

public sealed record TenantSlugAvailabilityResponse(
    string Slug,
    bool Available);
