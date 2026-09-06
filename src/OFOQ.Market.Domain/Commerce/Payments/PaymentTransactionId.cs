namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct PaymentTransactionId(Guid Value)
{
    public static PaymentTransactionId New() => new(Guid.NewGuid());

    public static PaymentTransactionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment transaction ID cannot be empty.",
                nameof(value));
        }

        return new PaymentTransactionId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
