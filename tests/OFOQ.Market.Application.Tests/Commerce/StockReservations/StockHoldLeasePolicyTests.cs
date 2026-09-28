using OFOQ.Market.Application.Commerce.StockReservations;

namespace OFOQ.Market.Application.Tests.Commerce.StockReservations;

public sealed class StockHoldLeasePolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewCartReservationExpiresAfterFifteenMinutes() =>
        Assert.Equal(Now.AddMinutes(15), StockHoldLeasePolicy.ExpiresAt(null, Now));

    [Fact]
    public void EditingCartDoesNotExtendUnexpiredReservation() =>
        Assert.Equal(Now.AddMinutes(2),
            StockHoldLeasePolicy.ExpiresAt(Now.AddMinutes(2), Now));

    [Fact]
    public void ExpiredReservationMayBeReacquiredOnlyThroughAvailabilityCheck() =>
        Assert.Equal(Now.AddMinutes(15),
            StockHoldLeasePolicy.ExpiresAt(Now.AddSeconds(-1), Now));

    [Fact]
    public void ExpiredCartAndOrderHoldsDoNotConsumeAvailability()
    {
        Assert.False(StockHoldLeasePolicy.CountsAgainstAvailability("Cart", Now, Now));
        Assert.False(StockHoldLeasePolicy.CountsAgainstAvailability("Order", Now.AddMinutes(-1), Now));
        Assert.True(StockHoldLeasePolicy.CountsAgainstAvailability("Order", Now.AddSeconds(1), Now));
    }

    [Fact]
    public void ProofReviewRetainsHoldUntilReviewed()
    {
        Assert.True(StockHoldLeasePolicy.CountsAgainstAvailability("Review", null, Now));
        Assert.False(StockHoldLeasePolicy.CountsAgainstAvailability("Released", null, Now));
        Assert.False(StockHoldLeasePolicy.CountsAgainstAvailability("Captured", null, Now));
    }
}
