using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Security;

public sealed class MfaSecurityServicesTests
{
    private const string TestConnectionString =
        "Host=127.0.0.1;Port=5432;Database=unused;Username=unused;Password=unused";

    private const string RfcTotpSecret =
        "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    [Fact]
    public void CreateEnrollment_GeneratesDistinctSecrets()
    {
        using var provider =
            CreateServiceProvider();

        var totpService =
            provider.GetRequiredService<ITotpService>();

        var first =
            totpService.CreateEnrollment(
                "user@example.com",
                "OFOQ.Market");

        var second =
            totpService.CreateEnrollment(
                "user@example.com",
                "OFOQ.Market");

        Assert.False(
            string.IsNullOrWhiteSpace(
                first.Secret));

        Assert.False(
            string.IsNullOrWhiteSpace(
                second.Secret));

        Assert.NotEqual(
            first.Secret,
            second.Secret);
    }

    [Fact]
    public void CreateEnrollment_ProducesAuthenticatorCompatibleUri()
    {
        using var provider =
            CreateServiceProvider();

        var totpService =
            provider.GetRequiredService<ITotpService>();

        var enrollment =
            totpService.CreateEnrollment(
                "user@example.com",
                "OFOQ.Market");

        Assert.StartsWith(
            "otpauth://totp/",
            enrollment.ProvisioningUri);

        Assert.Contains(
            $"secret={enrollment.Secret}",
            enrollment.ProvisioningUri);

        Assert.Contains(
            "issuer=OFOQ.Market",
            enrollment.ProvisioningUri);

        Assert.Contains(
            "algorithm=SHA1",
            enrollment.ProvisioningUri);

        Assert.Contains(
            "digits=6",
            enrollment.ProvisioningUri);

        Assert.Contains(
            "period=30",
            enrollment.ProvisioningUri);
    }

    [Fact]
    public void Verify_AcceptsKnownTotpVector()
    {
        using var provider =
            CreateServiceProvider();

        var totpService =
            provider.GetRequiredService<ITotpService>();

        var result =
            totpService.Verify(
                RfcTotpSecret,
                "287082",
                DateTimeOffset.FromUnixTimeSeconds(59));

        Assert.True(
            result.IsValid);

        Assert.Equal(
            1,
            result.TimeStep);
    }

    [Fact]
    public void Verify_AcceptsPreviousTimeStepWithinAllowedWindow()
    {
        using var provider =
            CreateServiceProvider();

        var totpService =
            provider.GetRequiredService<ITotpService>();

        var result =
            totpService.Verify(
                RfcTotpSecret,
                "287082",
                DateTimeOffset.FromUnixTimeSeconds(60));

        Assert.True(
            result.IsValid);

        Assert.Equal(
            1,
            result.TimeStep);
    }

    [Fact]
    public void Verify_RejectsCodeOutsideAllowedWindow()
    {
        using var provider =
            CreateServiceProvider();

        var totpService =
            provider.GetRequiredService<ITotpService>();

        var result =
            totpService.Verify(
                RfcTotpSecret,
                "287082",
                DateTimeOffset.FromUnixTimeSeconds(90));

        Assert.False(
            result.IsValid);

        Assert.Null(
            result.TimeStep);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("ABCDEF")]
    [InlineData("12 456")]
    public void Verify_RejectsMalformedCode(
        string code)
    {
        using var provider =
            CreateServiceProvider();

        var totpService =
            provider.GetRequiredService<ITotpService>();

        var result =
            totpService.Verify(
                RfcTotpSecret,
                code,
                DateTimeOffset.FromUnixTimeSeconds(59));

        Assert.False(
            result.IsValid);

        Assert.Null(
            result.TimeStep);
    }

    [Fact]
    public void SecretProtector_EncryptsAndRoundTripsSecret()
    {
        using var provider =
            CreateServiceProvider();

        var protector =
            provider.GetRequiredService<
                IMfaSecretProtector>();

        const string secret =
            "JBSWY3DPEHPK3PXP";

        var protectedSecret =
            protector.Protect(
                secret);

        Assert.False(
            string.IsNullOrWhiteSpace(
                protectedSecret));

        Assert.NotEqual(
            secret,
            protectedSecret);

        Assert.DoesNotContain(
            secret,
            protectedSecret);

        var restoredSecret =
            protector.Unprotect(
                protectedSecret);

        Assert.Equal(
            secret,
            restoredSecret);
    }

    [Fact]
    public void SecretProtector_RejectsTamperedProtectedSecret()
    {
        using var provider =
            CreateServiceProvider();

        var protector =
            provider.GetRequiredService<
                IMfaSecretProtector>();

        var protectedSecret =
            protector.Protect(
                "JBSWY3DPEHPK3PXP");

        var replacement =
            protectedSecret[0] == 'A'
                ? 'B'
                : 'A';

        var tamperedSecret =
            replacement +
            protectedSecret[1..];

        Assert.Throws<CryptographicException>(
            () =>
                protector.Unprotect(
                    tamperedSecret));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services =
            new ServiceCollection();

        services.AddInfrastructure(
            TestConnectionString);

        // الاختبارات تستخدم Key Ring مؤقتًا في الذاكرة فقط.
        // لا نكتب أو نقرأ مفاتيح Data Protection الحقيقية.
        services.RemoveAll<IDataProtectionProvider>();

        services.AddSingleton<IDataProtectionProvider>(
            new EphemeralDataProtectionProvider());

        return services.BuildServiceProvider();
    }
}