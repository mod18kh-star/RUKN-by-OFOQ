namespace OFOQ.Market.Domain.Catalog;

public readonly record struct Money
{
    private Money(
        decimal amount,
        CurrencyCode currency)
    {
        Amount =
            amount;

        Currency =
            currency;
    }

    public decimal Amount { get; }

    public CurrencyCode Currency { get; }

    public static Money Create(
        decimal amount,
        string currencyCode)
    {
        return Create(
            amount,
            CurrencyCode.Create(
                currencyCode));
    }

    public static Money Create(
        decimal amount,
        CurrencyCode currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Money amount cannot be negative.");
        }

        if (currency.IsEmpty)
        {
            throw new ArgumentException(
                "Currency is required.",
                nameof(currency));
        }

        return new Money(
            amount,
            currency);
    }

    public override string ToString()
    {
        return $"{Amount} {Currency.Value}";
    }
}