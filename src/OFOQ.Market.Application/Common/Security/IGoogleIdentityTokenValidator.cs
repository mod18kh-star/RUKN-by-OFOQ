namespace OFOQ.Market.Application.Common.Security;

public sealed record GoogleIdentityPrincipal(
    string Subject,
    string Email,
    bool EmailVerified);

public interface IGoogleIdentityTokenValidator
{
    Task<GoogleIdentityPrincipal> ValidateAsync(
        string idToken,
        string nonce,
        CancellationToken cancellationToken = default);
}

public sealed class InvalidGoogleIdentityTokenException : Exception
{
    public InvalidGoogleIdentityTokenException() : base("Google identity token is invalid.") { }
}

public sealed class GoogleIdentityProviderUnavailableException : Exception
{
    public GoogleIdentityProviderUnavailableException() : base("Google identity provider is unavailable.") { }
}
