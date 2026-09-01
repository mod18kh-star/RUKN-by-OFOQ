using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakeUnitOfWork :
    IUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }
}