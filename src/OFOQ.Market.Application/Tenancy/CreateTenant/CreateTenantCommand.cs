namespace OFOQ.Market.Application.Tenancy.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    Guid? CreatedByUserId = null);