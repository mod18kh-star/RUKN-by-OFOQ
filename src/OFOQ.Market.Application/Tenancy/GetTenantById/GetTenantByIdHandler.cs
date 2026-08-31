using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.GetTenantById;

public sealed class GetTenantByIdHandler
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantByIdHandler(
        ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<GetTenantByIdResult?> HandleAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        var tenant =
            await _tenantRepository.GetByIdAsync(
                tenantId,
                cancellationToken);

        if (tenant is null)
            return null;

        return new GetTenantByIdResult(
            tenant.Id,
            tenant.Name,
            tenant.Slug.Value,
            tenant.Status,
            tenant.CreatedAtUtc);
    }
}