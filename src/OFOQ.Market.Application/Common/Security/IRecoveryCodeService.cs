namespace OFOQ.Market.Application.Common.Security;

public interface IRecoveryCodeService
{
    IReadOnlyList<string> GenerateCodes(
        int count);

    string Hash(
        string code);

    bool Verify(
        string code,
        string codeHash);
}