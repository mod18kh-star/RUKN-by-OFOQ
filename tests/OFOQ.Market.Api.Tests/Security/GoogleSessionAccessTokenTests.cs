using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Security;

public sealed class GoogleSessionAccessTokenTests
{
    [Fact]
    public void Create_GoogleMfaSession_EmitsGoogleAndMfaWithoutPassword()
    {
        var service = new JwtAccessTokenService(
            new JwtSettings
            {
                Issuer = TestAuthenticationConstants.Issuer,
                Audience = TestAuthenticationConstants.Audience,
                SigningKey = TestAuthenticationConstants.SigningKey,
                AccessTokenMinutes = 15
            });

        var result = service.Create(
            UserId.New(),
            "owner@example.com",
            DateTimeOffset.UtcNow,
            AccessTokenAuthenticationLevel.MultiFactor,
            UserSessionAuthenticationMethod.Google);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        var methods = token.Claims
            .Where(claim => claim.Type == "amr")
            .Select(claim => claim.Value)
            .ToArray();

        Assert.Contains("google", methods);
        Assert.Contains("mfa", methods);
        Assert.DoesNotContain("pwd", methods);
    }
}
