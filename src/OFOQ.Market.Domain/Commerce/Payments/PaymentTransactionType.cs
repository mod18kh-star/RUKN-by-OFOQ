namespace OFOQ.Market.Domain.Commerce.Payments;

public enum PaymentTransactionType
{
    IntentCreated = 0,
    ProviderRequest = 1,
    ProviderConfirmation = 2,
    Failure = 3,
    Cancellation = 4,
    Expiration = 5
}
