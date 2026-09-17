using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Security;

public sealed record TrustedDeviceTokenMaterial(
    string Token,
    string TokenHash);

public interface ITrustedDeviceTokenService
{
    TrustedDeviceTokenMaterial Create(
        UserTrustedDeviceId trustedDeviceId);

    bool TryHash(
        string? token,
        out UserTrustedDeviceId trustedDeviceId,
        out string tokenHash);

    bool FixedTimeEquals(
        string leftHash,
        string rightHash);

    string HashUserAgent(
        string? userAgent);
}
