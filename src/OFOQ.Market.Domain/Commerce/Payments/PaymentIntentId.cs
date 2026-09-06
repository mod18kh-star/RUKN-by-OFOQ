namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct PaymentIntentId(Guid Value)
{
    public static PaymentIntentId New() => new(Guid.NewGuid());

    public static PaymentIntentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment intent ID cannot be empty.",
                nameof(value));
        }

        return new PaymentIntentId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
