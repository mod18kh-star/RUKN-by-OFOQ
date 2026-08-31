using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.CreateTenant;

public sealed class TenantSlugAlreadyExistsException :
    Exception
{
    public TenantSlugAlreadyExistsException(
        TenantSlug slug)
        : base(
            $"Tenant slug '{slug.Value}' is already in use.")
    {
        Slug = slug;
    }

    public TenantSlug Slug { get; }
}