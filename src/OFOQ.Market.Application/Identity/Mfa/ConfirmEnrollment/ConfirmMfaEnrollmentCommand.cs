using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.ConfirmEnrollment;

public sealed record ConfirmMfaEnrollmentCommand(
    UserId UserId,
    string Code);