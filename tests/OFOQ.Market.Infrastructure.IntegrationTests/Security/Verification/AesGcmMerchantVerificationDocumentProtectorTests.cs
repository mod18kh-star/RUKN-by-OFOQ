using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Security.Verification;

public sealed class AesGcmMerchantVerificationDocumentProtectorTests
{
    private const string EncryptionKeyConfigurationKey =
        "MerchantVerification:DocumentProtection:EncryptionKey";

    private const string FingerprintKeyConfigurationKey =
        "MerchantVerification:DocumentProtection:FingerprintKey";

    [Fact]
    public void ProtectAndUnprotect_RoundTripsDocumentNumberWithoutExposingPlaintext()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        var tenantId =
            TenantId.New();

        const string documentNumber =
            "109876543210";

        var result =
            protector.ProtectDocumentNumber(
                tenantId,
                MerchantVerificationDocumentType.NationalId,
                "SA",
                documentNumber);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ProtectedValue));

        Assert.StartsWith(
            "v1.",
            result.ProtectedValue,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            documentNumber,
            result.ProtectedValue,
            StringComparison.Ordinal);

        Assert.Equal(
            64,
            result.Fingerprint.Length);

        Assert.DoesNotContain(
            documentNumber,
            result.Fingerprint,
            StringComparison.Ordinal);

        var restored =
            protector.UnprotectDocumentNumber(
                tenantId,
                MerchantVerificationDocumentType.NationalId,
                "SA",
                result.ProtectedValue);

        Assert.Equal(
            documentNumber,
            restored);
    }

    [Fact]
    public void Fingerprint_SameLogicalDocumentNumber_IsStableAcrossFormattingAndCase()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        var first =
            protector.ComputeDocumentNumberFingerprint(
                MerchantVerificationDocumentType.Passport,
                "sa",
                " ab 123 456 ");

        var second =
            protector.ComputeDocumentNumberFingerprint(
                MerchantVerificationDocumentType.Passport,
                "SA",
                "AB123456");

        Assert.Equal(
            first,
            second);
    }

    [Fact]
    public void Fingerprint_SameNumberInDifferentCountry_IsDifferent()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        var saudiFingerprint =
            protector.ComputeDocumentNumberFingerprint(
                MerchantVerificationDocumentType.NationalId,
                "SA",
                "1234567890");

        var emiratesFingerprint =
            protector.ComputeDocumentNumberFingerprint(
                MerchantVerificationDocumentType.NationalId,
                "AE",
                "1234567890");

        Assert.NotEqual(
            saudiFingerprint,
            emiratesFingerprint);
    }

    [Fact]
    public void Fingerprint_SameNumberForDifferentDocumentType_IsDifferent()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        var identityFingerprint =
            protector.ComputeDocumentNumberFingerprint(
                MerchantVerificationDocumentType.NationalId,
                "SA",
                "1234567890");

        var commercialRegistrationFingerprint =
            protector.ComputeDocumentNumberFingerprint(
                MerchantVerificationDocumentType.CommercialRegistration,
                "SA",
                "1234567890");

        Assert.NotEqual(
            identityFingerprint,
            commercialRegistrationFingerprint);
    }

    [Fact]
    public void Fingerprint_DifferentSecretKey_ProducesDifferentFingerprint()
    {
        var firstProtector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        var secondProtector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateAlternativeFingerprintKey());

        using var firstDisposable =
            firstProtector as IDisposable;

        using var secondDisposable =
            secondProtector as IDisposable;

        var first =
            firstProtector.ComputeDocumentNumberFingerprint(
                MerchantVerificationDocumentType.NationalId,
                "SA",
                "1234567890");

        var second =
            secondProtector.ComputeDocumentNumberFingerprint(
                MerchantVerificationDocumentType.NationalId,
                "SA",
                "1234567890");

        Assert.NotEqual(
            first,
            second);
    }

    [Fact]
    public void Unprotect_WithDifferentTenant_FailsAuthentication()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        var originalTenantId =
            TenantId.New();

        var differentTenantId =
            TenantId.New();

        var result =
            protector.ProtectDocumentNumber(
                originalTenantId,
                MerchantVerificationDocumentType.NationalId,
                "SA",
                "1234567890");

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    protector.UnprotectDocumentNumber(
                        differentTenantId,
                        MerchantVerificationDocumentType.NationalId,
                        "SA",
                        result.ProtectedValue));

        Assert.Contains(
            "could not be authenticated",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unprotect_WithDifferentDocumentType_FailsAuthentication()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        var tenantId =
            TenantId.New();

        var result =
            protector.ProtectDocumentNumber(
                tenantId,
                MerchantVerificationDocumentType.NationalId,
                "SA",
                "1234567890");

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    protector.UnprotectDocumentNumber(
                        tenantId,
                        MerchantVerificationDocumentType.Passport,
                        "SA",
                        result.ProtectedValue));

        Assert.Contains(
            "could not be authenticated",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unprotect_WithDifferentCountry_FailsAuthentication()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        var tenantId =
            TenantId.New();

        var result =
            protector.ProtectDocumentNumber(
                tenantId,
                MerchantVerificationDocumentType.NationalId,
                "SA",
                "1234567890");

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    protector.UnprotectDocumentNumber(
                        tenantId,
                        MerchantVerificationDocumentType.NationalId,
                        "AE",
                        result.ProtectedValue));

        Assert.Contains(
            "could not be authenticated",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unprotect_WhenCiphertextIsTampered_FailsAuthentication()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        var tenantId =
            TenantId.New();

        var result =
            protector.ProtectDocumentNumber(
                tenantId,
                MerchantVerificationDocumentType.Passport,
                "SA",
                "A12345678");

        var parts =
            result.ProtectedValue.Split(
                '.',
                StringSplitOptions.None);

        Assert.Equal(
            4,
            parts.Length);

        var ciphertext =
            Convert.FromBase64String(
                parts[3]);

        Assert.NotEmpty(
            ciphertext);

        ciphertext[0] ^=
            0x01;

        var tampered =
            string.Join(
                '.',
                parts[0],
                parts[1],
                parts[2],
                Convert.ToBase64String(
                    ciphertext));

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    protector.UnprotectDocumentNumber(
                        tenantId,
                        MerchantVerificationDocumentType.Passport,
                        "SA",
                        tampered));

        Assert.Contains(
            "could not be authenticated",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenEncryptionKeyIsMissing_RejectsConfiguration()
    {
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        encryptionKey: null,
                        fingerprintKey:
                            CreateFingerprintKey()));

        Assert.Contains(
            EncryptionKeyConfigurationKey,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenFingerprintKeyIsMissing_RejectsConfiguration()
    {
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        CreateEncryptionKey(),
                        fingerprintKey: null));

        Assert.Contains(
            FingerprintKeyConfigurationKey,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenEncryptionKeyIsInvalidBase64_RejectsConfiguration()
    {
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        "not-valid-base64!!!",
                        CreateFingerprintKey()));

        Assert.Contains(
            "valid Base64",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenFingerprintKeyIsInvalidBase64_RejectsConfiguration()
    {
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        CreateEncryptionKey(),
                        "not-valid-base64!!!"));

        Assert.Contains(
            "valid Base64",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenEncryptionKeyIsNot32Bytes_RejectsConfiguration()
    {
        var shortKey =
            Convert.ToBase64String(
                Enumerable
                    .Range(
                        1,
                        16)
                    .Select(
                        value =>
                            (byte)value)
                    .ToArray());

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        shortKey,
                        CreateFingerprintKey()));

        Assert.Contains(
            "exactly 32 bytes",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenFingerprintKeyIsNot32Bytes_RejectsConfiguration()
    {
        var shortKey =
            Convert.ToBase64String(
                Enumerable
                    .Range(
                        1,
                        16)
                    .Select(
                        value =>
                            (byte)value)
                    .ToArray());

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        CreateEncryptionKey(),
                        shortKey));

        Assert.Contains(
            "exactly 32 bytes",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenBothKeysAreTheSame_RejectsConfiguration()
    {
        var key =
            CreateEncryptionKey();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        key,
                        key));

        Assert.Contains(
            "must be different",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComputeFingerprint_UnknownDocumentType_IsRejected()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                protector.ComputeDocumentNumberFingerprint(
                    MerchantVerificationDocumentType.Unknown,
                    "SA",
                    "1234567890"));
    }

    [Fact]
    public void Protect_InvalidCountryCode_IsRejected()
    {
        var protector =
            CreateProtector(
                CreateEncryptionKey(),
                CreateFingerprintKey());

        using var disposable =
            protector as IDisposable;

        Assert.Throws<ArgumentException>(
            () =>
                protector.ProtectDocumentNumber(
                    TenantId.New(),
                    MerchantVerificationDocumentType.NationalId,
                    "SAU",
                    "1234567890"));
    }

    private static string CreateEncryptionKey()
    {
        var bytes =
            Enumerable
                .Range(
                    1,
                    32)
                .Select(
                    value =>
                        (byte)value)
                .ToArray();

        return Convert.ToBase64String(
            bytes);
    }

    private static string CreateFingerprintKey()
    {
        var bytes =
            Enumerable
                .Range(
                    33,
                    32)
                .Select(
                    value =>
                        (byte)value)
                .ToArray();

        return Convert.ToBase64String(
            bytes);
    }

    private static string CreateAlternativeFingerprintKey()
    {
        var bytes =
            Enumerable
                .Range(
                    65,
                    32)
                .Select(
                    value =>
                        (byte)value)
                .ToArray();

        return Convert.ToBase64String(
            bytes);
    }

    private static IMerchantVerificationDocumentProtector
        CreateProtector(
            string? encryptionKey,
            string? fingerprintKey)
    {
        var settings =
            new Dictionary<string, string?>();

        if (encryptionKey is not null)
        {
            settings[
                EncryptionKeyConfigurationKey] =
                encryptionKey;
        }

        if (fingerprintKey is not null)
        {
            settings[
                FingerprintKeyConfigurationKey] =
                fingerprintKey;
        }

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    settings)
                .Build();

        var infrastructureAssembly =
            typeof(MarketDbContext)
                .Assembly;

        var protectorType =
            infrastructureAssembly.GetType(
                "OFOQ.Market.Infrastructure.Security.Verification.AesGcmMerchantVerificationDocumentProtector",
                throwOnError: true);

        Assert.NotNull(
            protectorType);

        try
        {
            var instance =
                Activator.CreateInstance(
                    protectorType!,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    args:
                    [
                        configuration
                    ],
                    culture: null);

            return Assert.IsAssignableFrom<
                IMerchantVerificationDocumentProtector>(
                    instance);
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo
                .Capture(
                    exception.InnerException)
                .Throw();

            throw;
        }
    }
}