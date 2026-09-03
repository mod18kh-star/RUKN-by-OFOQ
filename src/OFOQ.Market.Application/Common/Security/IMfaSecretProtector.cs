namespace OFOQ.Market.Application.Common.Security;

public interface IMfaSecretProtector
{
    string Protect(
        string secret);

    string Unprotect(
        string protectedSecret);
}