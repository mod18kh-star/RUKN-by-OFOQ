using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Security;

public sealed record RefreshTokenMaterial(
    string Token,
    string TokenHash);

public interface IRefreshTokenService
{
    RefreshTokenMaterial Create(
        UserSessionId sessionId);

    bool TryHash(
        string? token,
        out UserSessionId sessionId,
        out string tokenHash);

    bool FixedTimeEquals(
        string leftHash,
        string rightHash);
}
