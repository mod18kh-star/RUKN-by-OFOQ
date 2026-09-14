namespace OFOQ.Market.Contracts.Commerce.Orders;

public sealed record ShipOrderRequest(
    string ShippingCarrier,
    string TrackingNumber);