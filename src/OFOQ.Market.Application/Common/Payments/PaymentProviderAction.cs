namespace OFOQ.Market.Application.Common.Payments;

public sealed record PaymentProviderAction(
    PaymentProviderActionType Type,
    string Value);
