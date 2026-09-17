namespace OFOQ.Market.Application.Identity.EmailVerification.Confirm;

public sealed record ConfirmEmailVerificationCommand(
    string Token);
