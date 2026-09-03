using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Infrastructure.Persistence;

internal sealed class EfTransactionExecutor :
    ITransactionExecutor
{
    private readonly MarketDbContext _dbContext;

    public EfTransactionExecutor(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        var executionStrategy =
            _dbContext.Database
                .CreateExecutionStrategy();

        try
        {
            return await executionStrategy
                .ExecuteAsync(
                    async () =>
                    {
                        /*
                         * Every retry must start with a clean
                         * change tracker.
                         *
                         * The operation must load all entities
                         * it needs inside this callback.
                         */
                        _dbContext.ChangeTracker.Clear();

                        await using var transaction =
                            await _dbContext.Database
                                .BeginTransactionAsync(
                                    cancellationToken);

                        try
                        {
                            var result =
                                await operation(
                                    cancellationToken);

                            await transaction
                                .CommitAsync(
                                    cancellationToken);

                            return result;
                        }
                        catch
                        {
                            await transaction
                                .RollbackAsync(
                                    CancellationToken.None);

                            throw;
                        }
                    });
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _dbContext.ChangeTracker.Clear();

            throw new PersistenceConcurrencyException(
                "A concurrent database update prevented the operation from completing.",
                exception);
        }
    }
}