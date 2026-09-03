using OFOQ.Market.Infrastructure.Security;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Security;

public sealed class MfaLoginChallengeTokenServiceTests
{
    [Fact]
    public void Create_Generates256BitToken()
    {
        var service =
            new MfaLoginChallengeTokenService();

        var result =
            service.Create();

        Assert.Equal(
            64,
            result.Token.Length);

        Assert.All(
            result.Token,
            character =>
                Assert.True(
                    Uri.IsHexDigit(
                        character)));
    }

    [Fact]
    public void Create_GeneratesSha256Hash()
    {
        var service =
            new MfaLoginChallengeTokenService();

        var result =
            service.Create();

        Assert.Equal(
            64,
            result.TokenHash.Length);

        Assert.All(
            result.TokenHash,
            character =>
                Assert.True(
                    Uri.IsHexDigit(
                        character)));
    }

    [Fact]
    public void Create_GeneratesDistinctTokens()
    {
        var service =
            new MfaLoginChallengeTokenService();

        var first =
            service.Create();

        var second =
            service.Create();

        Assert.NotEqual(
            first.Token,
            second.Token);

        Assert.NotEqual(
            first.TokenHash,
            second.TokenHash);
    }

    [Fact]
    public void Create_DoesNotExposeTokenAsStoredHash()
    {
        var service =
            new MfaLoginChallengeTokenService();

        var result =
            service.Create();

        Assert.NotEqual(
            result.Token,
            result.TokenHash);
    }

    [Fact]
    public void TryHash_RecreatesStoredHash()
    {
        var service =
            new MfaLoginChallengeTokenService();

        var result =
            service.Create();

        var success =
            service.TryHash(
                result.Token,
                out var tokenHash);

        Assert.True(
            success);

        Assert.Equal(
            result.TokenHash,
            tokenHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ABC")]
    [InlineData(
        "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    public void TryHash_RejectsMalformedToken(
        string? token)
    {
        var service =
            new MfaLoginChallengeTokenService();

        var success =
            service.TryHash(
                token,
                out var tokenHash);

        Assert.False(
            success);

        Assert.Equal(
            string.Empty,
            tokenHash);
    }

    [Fact]
    public void TryHash_IsDeterministic()
    {
        var service =
            new MfaLoginChallengeTokenService();

        var result =
            service.Create();

        Assert.True(
            service.TryHash(
                result.Token,
                out var firstHash));

        Assert.True(
            service.TryHash(
                result.Token,
                out var secondHash));

        Assert.Equal(
            firstHash,
            secondHash);
    }
}