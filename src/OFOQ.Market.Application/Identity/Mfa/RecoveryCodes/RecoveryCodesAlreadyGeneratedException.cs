namespace OFOQ.Market.Application.Identity.Mfa.RecoveryCodes;

public sealed class RecoveryCodesAlreadyGeneratedException :
    Exception
{
    public RecoveryCodesAlreadyGeneratedException()
        : base(
            "Recovery codes have already been generated.")
    {
    }
}