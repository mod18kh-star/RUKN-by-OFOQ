namespace OFOQ.Market.Domain.Common;

public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; }
    Guid? CreatedByUserId { get; }

    DateTimeOffset? UpdatedAtUtc { get; }
    Guid? UpdatedByUserId { get; }
}
