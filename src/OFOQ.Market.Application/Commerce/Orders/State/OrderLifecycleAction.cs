namespace OFOQ.Market.Application.Commerce.Orders.State;

public enum OrderLifecycleAction
{
    Confirm = 0,

    StartProcessing = 1,

    ReadyToShip = 2,

    Ship = 3,

    MarkInTransit = 4,

    Deliver = 5
}