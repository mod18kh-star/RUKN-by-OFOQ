using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Consume;

public sealed record ConsumeRecoveryCodeCommand(
    UserId UserId,
    string Code);