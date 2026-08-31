namespace OFOQ.Market.Domain.Common;

public interface ITenantScoped<TTenantId>
    where TTenantId : notnull
{
    TTenantId TenantId { get; }
}