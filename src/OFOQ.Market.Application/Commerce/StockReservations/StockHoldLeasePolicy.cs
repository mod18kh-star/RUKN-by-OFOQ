namespace OFOQ.Market.Application.Commerce.StockReservations;

/// <summary>
/// Cart stock is reserved for 15 minutes, measured from the first successful
/// reservation, never silently prolonged on page refresh or cart edits.
/// Expired holds remain as audit rows but no longer reduce availability.
/// Manual payment proof transitions an acquired hold to an open-ended Review hold.
/// </summary>
public static class StockHoldLeasePolicy
{
    public static readonly TimeSpan CartDuration = TimeSpan.FromMinutes(15);

    public static DateTimeOffset ExpiresAt(DateTimeOffset? current, DateTimeOffset now) =>
        current is { } until && until > now ? until : now + CartDuration;

    public static bool CountsAgainstAvailability(string state, DateTimeOffset? expiresAt, DateTimeOffset now) =>
        state == "Review" ||
        (state is "Cart" or "Order" && expiresAt.HasValue && expiresAt.Value > now);
}
