namespace OFOQ.Market.Application.Common.Security;

public interface IPasswordHasher
{
    string Hash(
        string password);

    bool Verify(
        string passwordHash,
        string password);

    void PerformDummyVerification(
        string password);
}