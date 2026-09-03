using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakeTransactionExecutor :
    ITransactionExecutor
{
    public int ExecuteCount { get; private set; }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        ExecuteCount++;

        return await operation(
            cancellationToken);
    }
}