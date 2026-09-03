using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Generate;

public sealed record GenerateRecoveryCodesCommand(
    UserId UserId);