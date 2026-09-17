namespace OFOQ.Market.Domain.Content;

public readonly record struct ContentPageId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ContentPageId New() => new(Guid.NewGuid());
    public static ContentPageId From(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Content page ID cannot be empty.", nameof(value));
        return new ContentPageId(value);
    }
    public override string ToString() => Value.ToString();
}
