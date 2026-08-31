namespace OFOQ.Market.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
