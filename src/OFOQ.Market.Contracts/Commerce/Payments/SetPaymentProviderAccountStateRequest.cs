namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record SetPaymentProviderAccountStateRequest(
    bool Enabled);