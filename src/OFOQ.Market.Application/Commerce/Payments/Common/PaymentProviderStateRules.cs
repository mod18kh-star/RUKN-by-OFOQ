using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.Common;

internal static class PaymentProviderStateRules
{
    public static PaymentTransactionType Apply(
        PaymentIntent intent,
        PaymentIntentStatus providerStatus,
        string? providerReference,
        PaymentProviderAction? action,
        DateTimeOffset now,
        Guid? updatedByUserId)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var normalizedStatus =
            providerStatus == PaymentIntentStatus.Pending
                ? PaymentIntentStatus.Processing
                : providerStatus;


        switch (normalizedStatus)
        {
            case PaymentIntentStatus.RequiresAction:

                intent.MarkRequiresAction(
                    providerReference,
                    now,
                    updatedByUserId);


                if (action is not null)
                {
                    if (string.IsNullOrWhiteSpace(action.Value))
                    {
                        throw new PaymentProviderResultInvalidException(
                            "A provider action must include a value.");
                    }


                    intent.SetProviderAction(
                        MapActionType(action.Type),
                        action.Value,
                        now,
                        updatedByUserId);
                }


                return PaymentTransactionType.ProviderConfirmation;



            case PaymentIntentStatus.Processing:

                intent.MarkProcessing(
                    providerReference,
                    now,
                    updatedByUserId);


                return PaymentTransactionType.ProviderConfirmation;



            case PaymentIntentStatus.Succeeded:

                if (string.IsNullOrWhiteSpace(providerReference))
                {
                    throw new PaymentProviderResultInvalidException(
                        "A succeeded provider result must include a provider reference.");
                }


                intent.MarkSucceeded(
                    providerReference,
                    now,
                    updatedByUserId);


                return PaymentTransactionType.ProviderConfirmation;



            case PaymentIntentStatus.Failed:

                intent.MarkFailed(
                    providerReference,
                    now,
                    updatedByUserId);


                return PaymentTransactionType.Failure;



            case PaymentIntentStatus.Cancelled:

                intent.Cancel(
                    now,
                    updatedByUserId);


                return PaymentTransactionType.Cancellation;



            case PaymentIntentStatus.Expired:

                intent.Expire(
                    now,
                    updatedByUserId);


                return PaymentTransactionType.Expiration;



            default:

                throw new PaymentProviderResultInvalidException(
                    $"Unsupported provider payment status '{providerStatus}'.");
        }
    }



    private static PaymentIntentActionType MapActionType(
        PaymentProviderActionType actionType)
    {
        return actionType switch
        {
            PaymentProviderActionType.Redirect =>
                PaymentIntentActionType.Redirect,


            PaymentProviderActionType.Instructions =>
                PaymentIntentActionType.Instructions,


            _ =>
                throw new PaymentProviderResultInvalidException(
                    $"Unsupported provider action type '{actionType}'.")
        };
    }
}