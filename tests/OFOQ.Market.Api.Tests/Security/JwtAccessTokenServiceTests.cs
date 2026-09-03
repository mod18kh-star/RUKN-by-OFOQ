using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Security;

public sealed class JwtAccessTokenServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(
            2026,
            9,
            4,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Create_PasswordOnlyToken_ContainsOnlyPwdAmr()
    {
        var service =
            CreateService();

        var result =
            service.Create(
                UserId.New(),
                "user@example.com",
                FixedNow,
                AccessTokenAuthenticationLevel.PasswordOnly);

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    result.Token);

        var authenticationMethods =
            token.Claims
                .Where(
                    claim =>
                        claim.Type == "amr")
                .Select(
                    claim =>
                        claim.Value)
                .ToArray();

        Assert.Single(
            authenticationMethods);

        Assert.Contains(
            "pwd",
            authenticationMethods);

        Assert.DoesNotContain(
            "mfa",
            authenticationMethods);
    }

    [Fact]
    public void Create_MultiFactorToken_ContainsPwdAndMfaAmr()
    {
        var service =
            CreateService();

        var result =
            service.Create(
                UserId.New(),
                "user@example.com",
                FixedNow,
                AccessTokenAuthenticationLevel.MultiFactor);

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    result.Token);

        var authenticationMethods =
            token.Claims
                .Where(
                    claim =>
                        claim.Type == "amr")
                .Select(
                    claim =>
                        claim.Value)
                .ToArray();

        Assert.Equal(
            2,
            authenticationMethods.Length);

        Assert.Contains(
            "pwd",
            authenticationMethods);

        Assert.Contains(
            "mfa",
            authenticationMethods);
    }

    [Fact]
    public void Create_RejectsUnsupportedAuthenticationLevel()
    {
        var service =
            CreateService();

        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    service.Create(
                        UserId.New(),
                        "user@example.com",
                        FixedNow,
                        (AccessTokenAuthenticationLevel)999));
    }

    private static JwtAccessTokenService CreateService()
    {
        var settings =
            new JwtSettings
            {
                Issuer =
                    TestAuthenticationConstants.Issuer,

                Audience =
                    TestAuthenticationConstants.Audience,

                SigningKey =
                    TestAuthenticationConstants.SigningKey,

                AccessTokenMinutes =
                    15
            };

        settings.Validate();

        return new JwtAccessTokenService(
            settings);
    }
}