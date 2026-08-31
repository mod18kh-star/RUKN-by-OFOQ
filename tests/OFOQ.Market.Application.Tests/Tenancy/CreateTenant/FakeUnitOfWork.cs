using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Application.Tests.Tenancy.CreateTenant;

internal sealed class FakeUnitOfWork :
    IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;

        return Task.FromResult(1);
    }
}