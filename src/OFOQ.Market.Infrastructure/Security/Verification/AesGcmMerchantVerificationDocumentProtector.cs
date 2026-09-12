using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Security.Verification;

internal sealed class AesGcmMerchantVerificationDocumentProtector :
    IMerchantVerificationDocumentProtector,
    IDisposable
{
    private const string EncryptionKeyConfigurationKey =
        "MerchantVerification:DocumentProtection:EncryptionKey";

    private const string FingerprintKeyConfigurationKey =
        "MerchantVerification:DocumentProtection:FingerprintKey";

    private const string EnvelopeVersion =
        "v1";

    private const string FingerprintVersion =
        "v1";

    private const int NonceSize =
        12;

    private const int TagSize =
        16;

    private const int RequiredKeySize =
        32;

    private const int MaxDocumentNumberLength =
        256;

    private readonly byte[] _encryptionKey;

    private readonly byte[] _fingerprintKey;

    private bool _disposed;

    public AesGcmMerchantVerificationDocumentProtector(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(
            configuration);

        _encryptionKey =
            ReadKey(
                configuration,
                EncryptionKeyConfigurationKey);

        try
        {
            _fingerprintKey =
                ReadKey(
                    configuration,
                    FingerprintKeyConfigurationKey);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(
                _encryptionKey);

            throw;
        }

        if (CryptographicOperations.FixedTimeEquals(
                _encryptionKey,
                _fingerprintKey))
        {
            CryptographicOperations.ZeroMemory(
                _encryptionKey);

            CryptographicOperations.ZeroMemory(
                _fingerprintKey);

            throw new InvalidOperationException(
                "Merchant verification encryption and fingerprint keys must be different.");
        }
    }

    public ProtectedMerchantDocumentNumber ProtectDocumentNumber(
        TenantId tenantId,
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string documentNumber)
    {
        ThrowIfDisposed();

        ValidateTenantId(
            tenantId);

        ValidateDocumentType(
            documentType);

        var countryCode =
            NormalizeCountryCode(
                issuingCountryCode);

        var normalizedDocumentNumber =
            NormalizeDocumentNumberForStorage(
                documentNumber);

        var fingerprint =
            ComputeDocumentNumberFingerprint(
                documentType,
                countryCode,
                normalizedDocumentNumber);

        var plaintext =
            Encoding.UTF8.GetBytes(
                normalizedDocumentNumber);

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
                documentType,
                countryCode);

        try
        {
            using var aes =
                new AesGcm(
                    _encryptionKey,
                    TagSize);

            aes.Encrypt(
                nonce,
                plaintext,
                ciphertext,
                tag,
                additionalData);

            var protectedValue =
                string.Join(
                    '.',
                    EnvelopeVersion,
                    Convert.ToBase64String(
                        nonce),
                    Convert.ToBase64String(
                        tag),
                    Convert.ToBase64String(
                        ciphertext));

            return new ProtectedMerchantDocumentNumber(
                protectedValue,
                fingerprint);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                plaintext);

            CryptographicOperations.ZeroMemory(
                additionalData);
        }
    }

    public string UnprotectDocumentNumber(
        TenantId tenantId,
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string protectedDocumentNumber)
    {
        ThrowIfDisposed();

        ValidateTenantId(
            tenantId);

        ValidateDocumentType(
            documentType);

        var countryCode =
            NormalizeCountryCode(
                issuingCountryCode);

        if (string.IsNullOrWhiteSpace(
                protectedDocumentNumber))
        {
            throw new ArgumentException(
                "Protected merchant verification document number is required.",
                nameof(protectedDocumentNumber));
        }

        var parts =
            protectedDocumentNumber.Split(
                '.',
                StringSplitOptions.None);

        if (parts.Length != 4 ||
            !string.Equals(
                parts[0],
                EnvelopeVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Protected merchant verification document number uses an unsupported envelope.");
        }

        byte[] nonce;
        byte[] tag;
        byte[] ciphertext;

        try
        {
            nonce =
                Convert.FromBase64String(
                    parts[1]);

            tag =
                Convert.FromBase64String(
                    parts[2]);

            ciphertext =
                Convert.FromBase64String(
                    parts[3]);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Protected merchant verification document number is invalid.",
                exception);
        }

        if (nonce.Length != NonceSize ||
            tag.Length != TagSize)
        {
            throw new InvalidOperationException(
                "Protected merchant verification document number is invalid.");
        }

        var plaintext =
            new byte[ciphertext.Length];

        var additionalData =
            BuildAdditionalAuthenticatedData(
                tenantId,
                documentType,
                countryCode);

        try
        {
            using var aes =
                new AesGcm(
                    _encryptionKey,
                    TagSize);

            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintext,
                additionalData);

            return Encoding.UTF8.GetString(
                plaintext);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException(
                "Protected merchant verification document number could not be authenticated.",
                exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                plaintext);

            CryptographicOperations.ZeroMemory(
                additionalData);

            CryptographicOperations.ZeroMemory(
                nonce);

            CryptographicOperations.ZeroMemory(
                tag);

            CryptographicOperations.ZeroMemory(
                ciphertext);
        }
    }

    public string ComputeDocumentNumberFingerprint(
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string documentNumber)
    {
        ThrowIfDisposed();

        ValidateDocumentType(
            documentType);

        var countryCode =
            NormalizeCountryCode(
                issuingCountryCode);

        var normalizedDocumentNumber =
            NormalizeDocumentNumberForFingerprint(
                documentNumber);

        var material =
            $"{FingerprintVersion}|{countryCode}|{(int)documentType}|{normalizedDocumentNumber}";

        var materialBytes =
            Encoding.UTF8.GetBytes(
                material);

        byte[] hash;

        try
        {
            using var hmac =
                new HMACSHA256(
                    _fingerprintKey);

            hash =
                hmac.ComputeHash(
                    materialBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                materialBytes);
        }

        try
        {
            return Convert
                .ToHexString(
                    hash)
                .ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                hash);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(
            _encryptionKey);

        CryptographicOperations.ZeroMemory(
            _fingerprintKey);

        _disposed =
            true;
    }

    private static byte[] ReadKey(
        IConfiguration configuration,
        string configurationKey)
    {
        var configuredKey =
            configuration[
                configurationKey];

        if (string.IsNullOrWhiteSpace(
                configuredKey))
        {
            throw new InvalidOperationException(
                $"{configurationKey} must be configured.");
        }

        byte[] key;

        try
        {
            key =
                Convert.FromBase64String(
                    configuredKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"{configurationKey} must be a valid Base64 value.",
                exception);
        }

        if (key.Length !=
            RequiredKeySize)
        {
            CryptographicOperations.ZeroMemory(
                key);

            throw new InvalidOperationException(
                $"{configurationKey} must decode to exactly {RequiredKeySize} bytes.");
        }

        return key;
    }

    private static byte[] BuildAdditionalAuthenticatedData(
        TenantId tenantId,
        MerchantVerificationDocumentType documentType,
        string countryCode)
    {
        return Encoding.UTF8.GetBytes(
            $"merchant-verification-document|{EnvelopeVersion}|{tenantId.Value:N}|{countryCode}|{(int)documentType}");
    }

    private static string NormalizeCountryCode(
        string countryCode)
    {
        if (string.IsNullOrWhiteSpace(
                countryCode))
        {
            throw new ArgumentException(
                "Issuing country code is required.",
                nameof(countryCode));
        }

        var normalized =
            countryCode
                .Trim()
                .ToUpperInvariant();

        if (normalized.Length != 2 ||
            !normalized.All(
                char.IsLetter))
        {
            throw new ArgumentException(
                "Issuing country code must be a two-letter ISO country code.",
                nameof(countryCode));
        }

        return normalized;
    }

    private static string NormalizeDocumentNumberForStorage(
        string documentNumber)
    {
        if (string.IsNullOrWhiteSpace(
                documentNumber))
        {
            throw new ArgumentException(
                "Merchant verification document number is required.",
                nameof(documentNumber));
        }

        var normalized =
            documentNumber
                .Normalize(
                    NormalizationForm.FormKC)
                .Trim();

        if (normalized.Length == 0)
        {
            throw new ArgumentException(
                "Merchant verification document number is required.",
                nameof(documentNumber));
        }

        if (normalized.Length >
            MaxDocumentNumberLength)
        {
            throw new ArgumentException(
                $"Merchant verification document number cannot exceed {MaxDocumentNumberLength} characters.",
                nameof(documentNumber));
        }

        return normalized;
    }

    private static string NormalizeDocumentNumberForFingerprint(
        string documentNumber)
    {
        var normalized =
            NormalizeDocumentNumberForStorage(
                documentNumber);

        var builder =
            new StringBuilder(
                normalized.Length);

        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(
                    character))
            {
                continue;
            }

            builder.Append(
                char.ToUpperInvariant(
                    character));
        }

        if (builder.Length == 0)
        {
            throw new ArgumentException(
                "Merchant verification document number is required.",
                nameof(documentNumber));
        }

        return builder.ToString();
    }

    private static void ValidateTenantId(
        TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }
    }

    private static void ValidateDocumentType(
        MerchantVerificationDocumentType documentType)
    {
        if (documentType ==
                MerchantVerificationDocumentType.Unknown ||
            !Enum.IsDefined(
                documentType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(documentType),
                "A supported merchant verification document type is required.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}