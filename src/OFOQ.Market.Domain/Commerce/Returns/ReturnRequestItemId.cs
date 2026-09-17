namespace OFOQ.Market.Domain.Commerce.Returns;

public readonly record struct ReturnRequestItemId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ReturnRequestItemId New() => new(Guid.NewGuid());
    public static ReturnRequestItemId From(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Return request item ID cannot be empty.", nameof(value));
        return new ReturnRequestItemId(value);
    }
    public override string ToString() => Value.ToString();
}
