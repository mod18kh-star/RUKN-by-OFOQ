using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.EmailVerification.Start;

public sealed record StartEmailVerificationCommand(
    UserId UserId);
