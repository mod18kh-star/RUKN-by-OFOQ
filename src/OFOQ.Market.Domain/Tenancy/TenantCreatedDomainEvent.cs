using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Tenancy;

public sealed record TenantCreatedDomainEvent(
    TenantId TenantId,
    DateTimeOffset OccurredAtUtc
) : IDomainEvent;