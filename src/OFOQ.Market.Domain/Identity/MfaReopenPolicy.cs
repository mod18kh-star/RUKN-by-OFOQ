namespace OFOQ.Market.Domain.Identity;

public enum MfaReopenPolicy
{
    EveryBrowserSession = 0,
    Minutes15 = 15,
    Minutes30 = 30,
    Minutes60 = 60
}