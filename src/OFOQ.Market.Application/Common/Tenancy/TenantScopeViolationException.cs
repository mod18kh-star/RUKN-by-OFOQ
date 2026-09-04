namespace OFOQ.Market.Application.Common.Tenancy;

public sealed class TenantScopeViolationException :
    InvalidOperationException
{
    public TenantScopeViolationException(
        string message)
        : base(message)
    {
    }
}