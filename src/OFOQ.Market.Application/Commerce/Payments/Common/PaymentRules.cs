using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Payments.Common;

internal static class PaymentRules
{
    public const int MaximumIdempotencyKeyLength = 128;

    public static string NormalizeIdempotencyKey(
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency-Key is required.",
                nameof(idempotencyKey));
        }

        var normalized =
            idempotencyKey.Trim();

        if (normalized.Length > MaximumIdempotencyKeyLength)
        {
            throw new ArgumentException(
                $"Idempotency-Key cannot exceed {MaximumIdempotencyKeyLength} characters.",
                nameof(idempotencyKey));
        }

        return normalized;
    }

    public static void EnsureOrderIsPayable(
        Order? order,
        UserId customerUserId)
    {
        if (order is null ||
            order.CustomerUserId != customerUserId ||
            order.Status != OrderStatus.Pending)
        {
            throw new PaymentOrderNotAvailableException();
        }
    }

    public static void EnsurePaymentCanAcceptAttempt(
        Payment? payment)
    {
        if (payment?.Status == PaymentStatus.Succeeded)
        {
            throw new PaymentAlreadySucceededException();
        }

        if (payment?.Status == PaymentStatus.Cancelled)
        {
            throw new PaymentOrderNotAvailableException();
        }
    }

    public static bool IsProviderAttemptActive(
        PaymentIntentStatus status)
    {
        return status is
            PaymentIntentStatus.RequiresAction or
            PaymentIntentStatus.Processing;
    }

    public static void EnsureMethodAvailable(
        TenantPaymentMethod? method,
        decimal amount,
        string currency)
    {
        if (method is null ||
            !method.IsEnabled ||
            method.Type == PaymentMethodType.CashOnDelivery ||
            !string.Equals(
                method.Currency.Value,
                currency,
                StringComparison.OrdinalIgnoreCase) ||
            !method.SupportsAmount(amount))
        {
            throw new PaymentMethodNotAvailableException();
        }
    }

    public static void EnsureElectronicPaymentsAllowed(
        TenantPaymentMethod method,
        TenantPaymentCapability? capability)
    {
        if (method.Type == PaymentMethodType.Electronic &&
            capability is not null &&
            !capability.ElectronicPaymentsAllowed)
        {
            throw new ElectronicPaymentsSuspendedException();
        }
    }

    public static bool IsRetryable(
        PaymentIntentStatus status)
    {
        return status is
            PaymentIntentStatus.Failed or
            PaymentIntentStatus.Cancelled or
            PaymentIntentStatus.Expired;
    }

    public static PaymentIntentResult MapIntent(
        PaymentIntent intent,
        Payment payment,
        bool isIdempotentReplay)
    {
        return new PaymentIntentResult(
            intent.Id,
            payment.Id,
            payment.OrderId,
            intent.TenantPaymentMethodId,
            intent.Status.ToString(),
            intent.MethodType.ToString(),
            intent.ProviderCode.Value,
            intent.Amount,
            intent.Currency.Value,
            intent.ProviderReference,
            intent.CreatedAtUtc,
            isIdempotentReplay)
        {
            ActionType = intent.ActionType,
            ActionValue = intent.ActionValue
        };
    }
}