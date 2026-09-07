using System.Collections.Concurrent;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakePaymentProviderCredentialProtector :
    IPaymentProviderCredentialProtector
{
    private readonly ConcurrentDictionary<
        string,
        StoredCredentialPayload> _payloads =
        new(StringComparer.Ordinal);

    public string Protect(
        TenantId tenantId,
        TenantPaymentProviderAccountId accountId,
        PaymentProviderCredentialPayload payload)
    {
        ArgumentNullException.ThrowIfNull(
            payload);

        ValidateIdentifiers(
            tenantId,
            accountId);

        var token =
            $"test-protected:{Guid.NewGuid():N}";

        var storedPayload =
            new StoredCredentialPayload(
                tenantId,
                accountId,
                payload);

        if (!_payloads.TryAdd(
                token,
                storedPayload))
        {
            throw new InvalidOperationException(
                "Could not create protected test credential payload.");
        }

        return token;
    }

    public PaymentProviderCredentialPayload Unprotect(
        TenantId tenantId,
        TenantPaymentProviderAccountId accountId,
        string protectedPayload)
    {
        ValidateIdentifiers(
            tenantId,
            accountId);

        if (string.IsNullOrWhiteSpace(
                protectedPayload))
        {
            throw new ArgumentException(
                "Protected payment provider credentials are required.",
                nameof(protectedPayload));
        }

        if (!_payloads.TryGetValue(
                protectedPayload,
                out var stored))
        {
            throw new InvalidOperationException(
                "Protected test credential payload was not found.");
        }

        if (stored.TenantId != tenantId ||
            stored.AccountId != accountId)
        {
            throw new InvalidOperationException(
                "Protected credentials cannot be used by another tenant or payment provider account.");
        }

        return stored.Payload;
    }

    private static void ValidateIdentifiers(
        TenantId tenantId,
        TenantPaymentProviderAccountId accountId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (accountId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider account ID cannot be empty.",
                nameof(accountId));
        }
    }

    private sealed record StoredCredentialPayload(
        TenantId TenantId,
        TenantPaymentProviderAccountId AccountId,
        PaymentProviderCredentialPayload Payload);
}