using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Security.Payments;

internal sealed class AesGcmPaymentProviderCredentialProtector :
    IPaymentProviderCredentialProtector,
    IDisposable
{
    private const string ConfigurationKey =
        "Payments:CredentialProtection:MasterKey";

    private const string EnvelopeVersion =
        "v1";

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int RequiredKeySize = 32;

    private readonly byte[] _key;

    public AesGcmPaymentProviderCredentialProtector(
        IConfiguration configuration)
    {
        var configuredKey =
            configuration[ConfigurationKey];

        if (string.IsNullOrWhiteSpace(
                configuredKey))
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey} must be configured.");
        }

        try
        {
            _key =
                Convert.FromBase64String(
                    configuredKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey} must be a valid Base64 value.",
                exception);
        }

        if (_key.Length != RequiredKeySize)
        {
            CryptographicOperations.ZeroMemory(
                _key);

            throw new InvalidOperationException(
                $"{ConfigurationKey} must decode to exactly {RequiredKeySize} bytes.");
        }
    }

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

        var plaintext =
            JsonSerializer.SerializeToUtf8Bytes(
                payload.Values);

        var nonce =
            RandomNumberGenerator.GetBytes(
                NonceSize);

        var ciphertext =
            new byte[plaintext.Length];

        var tag =
            new byte[TagSize];

        var additionalData =
            BuildAdditionalAuthenticatedData(
                tenantId,
                accountId);

        try
        {
            using var aes =
                new AesGcm(
                    _key,
                    TagSize);

            aes.Encrypt(
                nonce,
                plaintext,
                ciphertext,
                tag,
                additionalData);

            return string.Join(
                '.',
                EnvelopeVersion,
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(tag),
                Convert.ToBase64String(ciphertext));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                plaintext);
        }
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

        var parts =
            protectedPayload.Split(
                '.',
                StringSplitOptions.None);

        if (parts.Length != 4 ||
            !string.Equals(
                parts[0],
                EnvelopeVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Protected payment provider credentials use an unsupported envelope.");
        }

        byte[] nonce;
        byte[] tag;
        byte[] ciphertext;

        try
        {
            nonce = Convert.FromBase64String(
                parts[1]);

            tag = Convert.FromBase64String(
                parts[2]);

            ciphertext = Convert.FromBase64String(
                parts[3]);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Protected payment provider credentials are invalid.",
                exception);
        }

        if (nonce.Length != NonceSize ||
            tag.Length != TagSize)
        {
            throw new InvalidOperationException(
                "Protected payment provider credentials are invalid.");
        }

        var plaintext =
            new byte[ciphertext.Length];

        var additionalData =
            BuildAdditionalAuthenticatedData(
                tenantId,
                accountId);

        try
        {
            using var aes =
                new AesGcm(
                    _key,
                    TagSize);

            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintext,
                additionalData);

            var values =
                JsonSerializer.Deserialize<Dictionary<string, string>>(
                    plaintext);

            if (values is null)
            {
                throw new InvalidOperationException(
                    "Protected payment provider credentials are invalid.");
            }

            return PaymentProviderCredentialPayload.Create(
                values);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException(
                "Protected payment provider credentials could not be authenticated.",
                exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Protected payment provider credentials contain invalid data.",
                exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                plaintext);
        }
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(
            _key);
    }

    private static byte[] BuildAdditionalAuthenticatedData(
        TenantId tenantId,
        TenantPaymentProviderAccountId accountId)
    {
        return Encoding.UTF8.GetBytes(
            $"{tenantId.Value:N}:{accountId.Value:N}");
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
}