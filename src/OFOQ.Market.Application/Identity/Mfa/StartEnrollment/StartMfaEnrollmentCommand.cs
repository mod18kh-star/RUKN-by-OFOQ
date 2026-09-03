using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.StartEnrollment;

public sealed record StartMfaEnrollmentCommand(
    UserId UserId);