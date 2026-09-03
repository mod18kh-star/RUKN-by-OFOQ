using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace OFOQ.Market.Api.Tests.Support;

internal static class TestJwtTokenFactory
{
    public static string Create(
        Guid userId,
        string email,
        DateTimeOffset notBeforeUtc,
        DateTimeOffset expiresAtUtc,
        bool useCorrectSigningKey = true)
    {
        var signingKeyBytes =
            useCorrectSigningKey
                ? Convert.FromBase64String(
                    TestAuthenticationConstants.SigningKey)
                : CreateWrongSigningKey();

        var securityKey =
            new SymmetricSecurityKey(
                signingKeyBytes);

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    userId.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    email),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

        var token =
            new JwtSecurityToken(
                issuer:
                    TestAuthenticationConstants.Issuer,

                audience:
                    TestAuthenticationConstants.Audience,

                claims:
                    claims,

                notBefore:
                    notBeforeUtc.UtcDateTime,

                expires:
                    expiresAtUtc.UtcDateTime,

                signingCredentials:
                    credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private static byte[] CreateWrongSigningKey()
    {
        var bytes =
            new byte[64];

        Array.Fill(
            bytes,
            (byte)0xA5);

        return bytes;
    }
}