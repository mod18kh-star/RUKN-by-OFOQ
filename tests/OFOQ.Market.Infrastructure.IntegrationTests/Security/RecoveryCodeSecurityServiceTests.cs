using OFOQ.Market.Infrastructure.Security;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Security;

public sealed class RecoveryCodeSecurityServiceTests
{
    private const string TestKey =
        "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QEA=";

    [Fact]
    public void GenerateCodes_GeneratesRequestedNumberOfUniqueCodes()
    {
        var service =
            CreateService();

        var codes =
            service.GenerateCodes(
                8);

        Assert.Equal(
            8,
            codes.Count);

        Assert.Equal(
            8,
            codes.Distinct().Count());
    }

    [Fact]
    public void GenerateCodes_UsesExpectedFormat()
    {
        var service =
            CreateService();

        var code =
            Assert.Single(
                service.GenerateCodes(
                    1));

        var groups =
            code.Split('-');

        Assert.Equal(
            5,
            groups.Length);

        Assert.All(
            groups,
            group =>
                Assert.Equal(
                    4,
                    group.Length));
    }

    [Fact]
    public void Hash_DoesNotContainOriginalRecoveryCode()
    {
        var service =
            CreateService();

        var code =
            Assert.Single(
                service.GenerateCodes(
                    1));

        var hash =
            service.Hash(
                code);

        Assert.NotEqual(
            code,
            hash);

        Assert.DoesNotContain(
            code,
            hash);
    }

    [Fact]
    public void Verify_AcceptsCorrectCode()
    {
        var service =
            CreateService();

        var code =
            Assert.Single(
                service.GenerateCodes(
                    1));

        var hash =
            service.Hash(
                code);

        Assert.True(
            service.Verify(
                code,
                hash));
    }

    [Fact]
    public void Verify_RejectsIncorrectCode()
    {
        var service =
            CreateService();

        var code =
            Assert.Single(
                service.GenerateCodes(
                    1));

        var hash =
            service.Hash(
                code);

        var differentCode =
            Assert.Single(
                service.GenerateCodes(
                    1));

        Assert.False(
            service.Verify(
                differentCode,
                hash));
    }

    [Fact]
    public void Verify_AcceptsLowercaseAndRemovedSeparators()
    {
        var service =
            CreateService();

        var code =
            Assert.Single(
                service.GenerateCodes(
                    1));

        var hash =
            service.Hash(
                code);

        var normalizedInput =
            code
                .Replace(
                    "-",
                    string.Empty)
                .ToLowerInvariant();

        Assert.True(
            service.Verify(
                normalizedInput,
                hash));
    }

    [Fact]
    public void Hash_IsDeterministicForSameCodeAndKey()
    {
        var service =
            CreateService();

        var code =
            Assert.Single(
                service.GenerateCodes(
                    1));

        Assert.Equal(
            service.Hash(code),
            service.Hash(code));
    }

    [Fact]
    public void DifferentHmacKey_CannotVerifyExistingHash()
    {
        var first =
            CreateService();

        var second =
            new HmacRecoveryCodeService(
                "QEBAOD08Ozo5ODc2NTQzMjEwLysrLy8vLy8vLy8vLy8vLy8vLy8vLy8vLy8vLy8vLy8vLy8vLy8vLy8vLy8vLw==");

        var code =
            Assert.Single(
                first.GenerateCodes(
                    1));

        var hash =
            first.Hash(
                code);

        Assert.False(
            second.Verify(
                code,
                hash));
    }

    private static HmacRecoveryCodeService CreateService()
    {
        return new HmacRecoveryCodeService(
            TestKey);
    }
}