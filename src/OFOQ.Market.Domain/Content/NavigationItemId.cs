namespace OFOQ.Market.Domain.Content;

public readonly record struct NavigationItemId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static NavigationItemId New() => new(Guid.NewGuid());
    public static NavigationItemId From(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Navigation item ID cannot be empty.", nameof(value));
        return new NavigationItemId(value);
    }
    public override string ToString() => Value.ToString();
}
