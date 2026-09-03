using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakePasswordHasher :
    IPasswordHasher
{
    public string Hash(
        string password)
    {
        return $"TEST-HASH::{password}";
    }

    public bool Verify(
        string passwordHash,
        string password)
    {
        return passwordHash ==
            $"TEST-HASH::{password}";
    }

    public void PerformDummyVerification(
        string password)
    {
    }
}