using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed record UserRegisteredDomainEvent(
    UserId UserId,
    EmailAddress Email,
    DateTimeOffset OccurredAtUtc
) : IDomainEvent;
