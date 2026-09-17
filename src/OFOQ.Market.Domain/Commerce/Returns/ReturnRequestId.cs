namespace OFOQ.Market.Domain.Commerce.Returns;

public readonly record struct ReturnRequestId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ReturnRequestId New() => new(Guid.NewGuid());
    public static ReturnRequestId From(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Return request ID cannot be empty.", nameof(value));
        return new ReturnRequestId(value);
    }
    public override string ToString() => Value.ToString();
}
