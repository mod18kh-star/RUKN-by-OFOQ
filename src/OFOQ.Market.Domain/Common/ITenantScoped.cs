namespace OFOQ.Market.Domain.Common;

public interface ITenantScoped
{
    Guid TenantId { get; }
}
