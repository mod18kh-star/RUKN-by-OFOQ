namespace OFOQ.Market.Domain.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAtUtc { get; }
    Guid? DeletedByUserId { get; }
}
