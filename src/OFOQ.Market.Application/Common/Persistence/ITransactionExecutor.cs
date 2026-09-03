namespace OFOQ.Market.Application.Common.Persistence;

public interface ITransactionExecutor
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}