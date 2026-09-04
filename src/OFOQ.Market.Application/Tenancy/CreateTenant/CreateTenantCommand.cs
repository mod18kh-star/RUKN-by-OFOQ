using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Tenancy.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    UserId CreatorUserId);