using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Tenancy;

public sealed record TenantSlugChangedDomainEvent(
    TenantId TenantId,
    TenantSlug PreviousSlug,
    TenantSlug NewSlug,
    DateTimeOffset OccurredAtUtc
) : IDomainEvent;