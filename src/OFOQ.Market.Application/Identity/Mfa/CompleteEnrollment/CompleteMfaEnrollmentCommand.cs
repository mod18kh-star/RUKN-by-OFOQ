using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.CompleteEnrollment;

public sealed record CompleteMfaEnrollmentCommand(
    UserId UserId,
    string Code);
