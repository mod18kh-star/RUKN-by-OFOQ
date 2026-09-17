using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Infrastructure.Security;

public sealed class GoogleIdentityTokenValidator :
    IGoogleIdentityTokenValidator
{
    private static readonly string[] ValidIssuers =
    [
        "https://accounts.google.com",
        "accounts.google.com"
    ];

    private readonly GoogleIdentityOptions _options;

    private readonly IConfigurationManager<OpenIdConnectConfiguration>
        _configurationManager;

    private readonly JwtSecurityTokenHandler _tokenHandler =
        new()
        {
            MapInboundClaims = false
        };

    public GoogleIdentityTokenValidator(
        GoogleIdentityOptions options)
    {
        _options =
            options;

        _configurationManager =
            new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://accounts.google.com/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever
                {
                    RequireHttps = true
                });
    }

    public async Task<GoogleIdentityPrincipal> ValidateAsync(
        string idToken,
        string nonce,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled ||
            string.IsNullOrWhiteSpace(
                _options.ClientId))
        {
            throw new GoogleIdentityProviderUnavailableException();
        }

        if (string.IsNullOrWhiteSpace(
                idToken) ||
            string.IsNullOrWhiteSpace(
                nonce))
        {
            throw new InvalidGoogleIdentityTokenException();
        }

        OpenIdConnectConfiguration configuration;

        try
        {
            configuration =
                await _configurationManager
                    .GetConfigurationAsync(
                        cancellationToken);
        }
        catch (Exception exception) when (
            exception is IOException or
            HttpRequestException or
            InvalidOperationException)
        {
            throw new GoogleIdentityProviderUnavailableException();
        }

        try
        {
            var validationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = ValidIssuers,
                    ValidateAudience = true,
                    ValidAudience = _options.ClientId,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = configuration.SigningKeys,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = "sub"
                };

            var validationResult =
                await _tokenHandler.ValidateTokenAsync(
                    idToken,
                    validationParameters);

            if (!validationResult.IsValid ||
                validationResult.ClaimsIdentity is null)
            {
                throw new InvalidGoogleIdentityTokenException();
            }

            var identity =
                validationResult.ClaimsIdentity;

            var subject =
                identity.FindFirst("sub")?.Value;

            var email =
                identity.FindFirst("email")?.Value;

            var emailVerifiedValue =
                identity.FindFirst("email_verified")?.Value;

            var tokenNonce =
                identity.FindFirst("nonce")?.Value;

            var authorizedParty =
                identity.FindFirst("azp")?.Value;

            if (string.IsNullOrWhiteSpace(
                    subject) ||
                subject.Length > 255 ||
                string.IsNullOrWhiteSpace(
                    email) ||
                !bool.TryParse(
                    emailVerifiedValue,
                    out var emailVerified) ||
                !emailVerified ||
                string.IsNullOrWhiteSpace(
                    tokenNonce) ||
                !FixedTimeEquals(
                    tokenNonce,
                    nonce))
            {
                throw new InvalidGoogleIdentityTokenException();
            }

            if (!string.IsNullOrWhiteSpace(
                    authorizedParty) &&
                !string.Equals(
                    authorizedParty,
                    _options.ClientId,
                    StringComparison.Ordinal))
            {
                throw new InvalidGoogleIdentityTokenException();
            }

            return new GoogleIdentityPrincipal(
                subject,
                email,
                true);
        }
        catch (InvalidGoogleIdentityTokenException)
        {
            throw;
        }
        catch (SecurityTokenException)
        {
            throw new InvalidGoogleIdentityTokenException();
        }
        catch (ArgumentException)
        {
            throw new InvalidGoogleIdentityTokenException();
        }
    }

    private static bool FixedTimeEquals(
        string left,
        string right)
    {
        var leftBytes =
            Encoding.UTF8.GetBytes(
                left);

        var rightBytes =
            Encoding.UTF8.GetBytes(
                right);

        if (leftBytes.Length !=
            rightBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            leftBytes,
            rightBytes);
    }
}
