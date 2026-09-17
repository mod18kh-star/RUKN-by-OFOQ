namespace OFOQ.Market.Contracts.Identity;

public sealed record TrustedDeviceResponse(
    Guid DeviceId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastUsedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? CreatedIpAddress,
    string? LastIpAddress,
    bool IsRevoked);
