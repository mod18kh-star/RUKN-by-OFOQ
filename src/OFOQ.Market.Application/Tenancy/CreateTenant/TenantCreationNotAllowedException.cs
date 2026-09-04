namespace OFOQ.Market.Application.Tenancy.CreateTenant;

public sealed class TenantCreationNotAllowedException :
    Exception
{
    public TenantCreationNotAllowedException()
        : base(
            "The authenticated user is not allowed to create a tenant.")
    {
    }
}