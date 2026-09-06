namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId New() => new(Guid.NewGuid());

    public static PaymentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment ID cannot be empty.",
                nameof(value));
        }

        return new PaymentId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
