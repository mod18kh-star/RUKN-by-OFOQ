namespace OFOQ.Market.Application.Common.Security;

public sealed record EmailVerificationToken(
    string Token,
    string TokenHash);

public interface IEmailVerificationTokenService
{
    EmailVerificationToken Create();

    bool TryHash(
        string? token,
        out string tokenHash);
}
