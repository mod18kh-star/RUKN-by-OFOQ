using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Security.Payments;

public sealed class AesGcmPaymentProviderCredentialProtectorTests
{
    private const string ConfigurationKey =
        "Payments:CredentialProtection:MasterKey";

    [Fact]
    public void ProtectAndUnprotect_RoundTripsCredentialsWithoutExposingPlaintext()
    {
        var protector =
            CreateProtector(
                CreateValidMasterKey());

        using var disposable =
            protector as IDisposable;

        var tenantId =
            TenantId.New();

        var accountId =
            TenantPaymentProviderAccountId.From(
                Guid.NewGuid());

        const string apiKey =
            "super-secret-api-key";

        const string merchantId =
            "merchant-123456";

        var payload =
            PaymentProviderCredentialPayload.Create(
                new Dictionary<string, string>
                {
                    ["api_key"] =
                        apiKey,

                    ["merchant_id"] =
                        merchantId
                });

        var protectedPayload =
            protector.Protect(
                tenantId,
                accountId,
                payload);

        Assert.False(
            string.IsNullOrWhiteSpace(
                protectedPayload));

        Assert.StartsWith(
            "v1.",
            protectedPayload,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            apiKey,
            protectedPayload,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            merchantId,
            protectedPayload,
            StringComparison.Ordinal);

        var restored =
            protector.Unprotect(
                tenantId,
                accountId,
                protectedPayload);

        Assert.Equal(
            apiKey,
            restored.Values["api_key"]);

        Assert.Equal(
            merchantId,
            restored.Values["merchant_id"]);

        Assert.Equal(
            2,
            restored.Values.Count);
    }

    [Fact]
    public void Unprotect_WithDifferentTenant_FailsAuthentication()
    {
        var protector =
            CreateProtector(
                CreateValidMasterKey());

        using var disposable =
            protector as IDisposable;

        var originalTenantId =
            TenantId.New();

        var differentTenantId =
            TenantId.New();

        var accountId =
            TenantPaymentProviderAccountId.From(
                Guid.NewGuid());

        var payload =
            CreatePayload();

        var protectedPayload =
            protector.Protect(
                originalTenantId,
                accountId,
                payload);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    protector.Unprotect(
                        differentTenantId,
                        accountId,
                        protectedPayload));

        Assert.Contains(
            "could not be authenticated",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unprotect_WithDifferentAccount_FailsAuthentication()
    {
        var protector =
            CreateProtector(
                CreateValidMasterKey());

        using var disposable =
            protector as IDisposable;

        var tenantId =
            TenantId.New();

        var originalAccountId =
            TenantPaymentProviderAccountId.From(
                Guid.NewGuid());

        var differentAccountId =
            TenantPaymentProviderAccountId.From(
                Guid.NewGuid());

        var payload =
            CreatePayload();

        var protectedPayload =
            protector.Protect(
                tenantId,
                originalAccountId,
                payload);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    protector.Unprotect(
                        tenantId,
                        differentAccountId,
                        protectedPayload));

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
                CreateValidMasterKey());

        using var disposable =
            protector as IDisposable;

        var tenantId =
            TenantId.New();

        var accountId =
            TenantPaymentProviderAccountId.From(
                Guid.NewGuid());

        var protectedPayload =
            protector.Protect(
                tenantId,
                accountId,
                CreatePayload());

        var parts =
            protectedPayload.Split(
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

        var tamperedPayload =
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
                    protector.Unprotect(
                        tenantId,
                        accountId,
                        tamperedPayload));

        Assert.Contains(
            "could not be authenticated",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenMasterKeyIsMissing_RejectsConfiguration()
    {
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        configuredKey: null));

        Assert.Contains(
            ConfigurationKey,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenMasterKeyIsInvalidBase64_RejectsConfiguration()
    {
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CreateProtector(
                        "not-valid-base64!!!"));

        Assert.Contains(
            "valid Base64",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenMasterKeyIsNot32Bytes_RejectsConfiguration()
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
                        shortKey));

        Assert.Contains(
            "exactly 32 bytes",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static PaymentProviderCredentialPayload
        CreatePayload()
    {
        return PaymentProviderCredentialPayload.Create(
            new Dictionary<string, string>
            {
                ["api_key"] =
                    "integration-secret",

                ["merchant_id"] =
                    "integration-merchant"
            });
    }

    private static string CreateValidMasterKey()
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

    private static IPaymentProviderCredentialProtector
        CreateProtector(
            string? configuredKey)
    {
        var settings =
            new Dictionary<string, string?>();

        if (configuredKey is not null)
        {
            settings[ConfigurationKey] =
                configuredKey;
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
                "OFOQ.Market.Infrastructure.Security.Payments.AesGcmPaymentProviderCredentialProtector",
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
                IPaymentProviderCredentialProtector>(
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