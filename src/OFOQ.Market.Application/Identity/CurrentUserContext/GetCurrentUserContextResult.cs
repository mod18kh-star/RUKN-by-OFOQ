using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.CurrentUserContext;

public sealed record GetCurrentUserContextResult(
    UserId UserId,
    string Email,
    UserStatus Status,
    bool EmailVerified,
    bool MfaEnabled,
    IReadOnlyList<PlatformRole> PlatformRoles,
    bool HasTenantMemberships,
    string? FullName = null,
    string? PhoneNumber = null);
