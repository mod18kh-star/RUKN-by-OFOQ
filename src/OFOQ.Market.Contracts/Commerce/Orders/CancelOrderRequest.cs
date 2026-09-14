namespace OFOQ.Market.Contracts.Commerce.Orders;

public sealed record CancelOrderRequest(
    string? Reason);