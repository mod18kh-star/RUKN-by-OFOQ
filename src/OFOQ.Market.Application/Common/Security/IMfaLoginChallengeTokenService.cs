namespace OFOQ.Market.Application.Common.Security;

public sealed record MfaLoginChallengeToken(
    string Token,
    string TokenHash);

public interface IMfaLoginChallengeTokenService
{
    MfaLoginChallengeToken Create();

    bool TryHash(
        string? token,
        out string tokenHash);
}