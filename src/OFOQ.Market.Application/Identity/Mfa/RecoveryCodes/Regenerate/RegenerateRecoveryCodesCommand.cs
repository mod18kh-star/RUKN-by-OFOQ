using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Regenerate;

public sealed record RegenerateRecoveryCodesCommand(
    UserId UserId);