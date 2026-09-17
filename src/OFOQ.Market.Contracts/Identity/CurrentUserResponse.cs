namespace OFOQ.Market.Contracts.Identity;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string Status,
    bool EmailVerified,
    bool MfaEnabled,
    bool SessionMfaVerified,
    IReadOnlyList<string> AuthenticationMethods,
    IReadOnlyList<string> PlatformRoles,
    bool HasTenantMemberships,
    Guid? SessionId,
    string? FullName = null,
    string? PhoneNumber = null);
