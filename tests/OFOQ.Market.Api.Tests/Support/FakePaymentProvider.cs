using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakePaymentProvider :
    IPaymentProvider,
    IPaymentWebhookProvider
{
    public PaymentProviderCode ProviderCode =>
        PaymentProviderCode.Create("provider-a");

    public int CreatePaymentCallCount { get; private set; }

    public int WebhookCallCount { get; private set; }

    public PaymentProviderResult NextPaymentResult { get; set; } =
        new(
            PaymentIntentStatus.RequiresAction,
            "provider-reference-1",
            new PaymentProviderAction(
                PaymentProviderActionType.Redirect,
                "https://payments.example.test/continue"));

    public PaymentWebhookResult NextWebhookResult { get; set; } =
        new(
            true,
            "event-1",
            PaymentIntentStatus.Processing,
            "provider-reference-1");

    public Exception? CreatePaymentException { get; set; }

    public Task<PaymentProviderResult> CreatePaymentAsync(
        PaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        CreatePaymentCallCount++;

        if (CreatePaymentException is not null)
        {
            throw CreatePaymentException;
        }

        return Task.FromResult(NextPaymentResult);
    }

    public Task<PaymentWebhookResult> ParseAndValidateWebhookAsync(
        PaymentWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        WebhookCallCount++;
        return Task.FromResult(NextWebhookResult);
    }

    public void Reset()
    {
        CreatePaymentCallCount = 0;
        WebhookCallCount = 0;
        CreatePaymentException = null;
        NextPaymentResult = new PaymentProviderResult(
            PaymentIntentStatus.RequiresAction,
            "provider-reference-1",
            new PaymentProviderAction(
                PaymentProviderActionType.Redirect,
                "https://payments.example.test/continue"));
        NextWebhookResult = new PaymentWebhookResult(
            true,
            "event-1",
            PaymentIntentStatus.Processing,
            "provider-reference-1");
    }
}
