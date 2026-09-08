using OFOQ.Market.Application.Commerce.Configuration.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Commerce.Configuration.ConfigureVertical;

public sealed class ConfigureCommerceVerticalHandler
{
    private readonly ITenantCommerceVerticalRepository
        _verticalRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly ITransactionExecutor
        _transactionExecutor;

    private readonly TimeProvider
        _timeProvider;

    public ConfigureCommerceVerticalHandler(
        ITenantCommerceVerticalRepository verticalRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        ITransactionExecutor transactionExecutor,
        TimeProvider timeProvider)
    {
        _verticalRepository =
            verticalRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _transactionExecutor =
            transactionExecutor;

        _timeProvider =
            timeProvider;
    }

    public Task<CommerceVerticalResult> HandleAsync(
        ConfigureCommerceVerticalCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        CommerceConfigurationRules.EnsureActor(
            command.ActorUserId);

        CommerceConfigurationRules.EnsureVerticalType(
            command.VerticalType);

        if (!command.IsEnabled &&
            command.IsPrimary)
        {
            throw new CommerceConfigurationConflictException(
                "A disabled commerce vertical cannot be primary.");
        }

        var tenantId =
            CommerceConfigurationRules.GetRequiredTenantId(
                _currentTenant);

        return _transactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                /*
                 * Everything required for this operation must be
                 * loaded inside the transaction callback.
                 *
                 * EfTransactionExecutor may clear the change tracker
                 * and retry the callback when the provider execution
                 * strategy requires it.
                 */
                var verticals =
                    await _verticalRepository.GetAllAsync(
                        transactionCancellationToken);

                var existing =
                    verticals.SingleOrDefault(
                        vertical =>
                            vertical.VerticalType ==
                            command.VerticalType);

                var now =
                    _timeProvider.GetUtcNow();

                if (existing is null)
                {
                    existing =
                        await ConfigureNewVerticalAsync(
                            tenantId,
                            verticals,
                            command,
                            now,
                            transactionCancellationToken);
                }
                else
                {
                    existing =
                        await ConfigureExistingVerticalAsync(
                            tenantId,
                            verticals,
                            existing,
                            command,
                            now,
                            transactionCancellationToken);
                }

                var definition =
                    CommerceVerticalCatalog.Get(
                        existing.VerticalType);

                return new CommerceVerticalResult(
                    existing.VerticalType,
                    definition.Code,
                    existing.IsEnabled,
                    existing.IsPrimary);
            },
            cancellationToken);
    }

    private async Task<TenantCommerceVertical>
        ConfigureNewVerticalAsync(
            Domain.Tenancy.TenantId tenantId,
            IReadOnlyCollection<TenantCommerceVertical> verticals,
            ConfigureCommerceVerticalCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        if (!command.IsEnabled)
        {
            throw new CommerceConfigurationConflictException(
                "A commerce vertical must be enabled when it is first configured.");
        }

        var hasPrimary =
            verticals.Any(
                vertical =>
                    vertical.IsEnabled &&
                    vertical.IsPrimary);

        if (!hasPrimary &&
            !command.IsPrimary)
        {
            throw new CommerceConfigurationConflictException(
                "The first enabled commerce vertical must be primary.");
        }

        /*
         * PostgreSQL protects the invariant with the partial
         * unique index:
         *
         * ux_commerce_verticals_tenant_primary
         *
         * We must therefore persist the demotion of the old
         * primary before persisting the new primary.
         *
         * Both SaveChanges calls remain inside the same database
         * transaction, so the temporary no-primary state is never
         * committed or externally visible.
         */
        if (command.IsPrimary)
        {
            var demotedExistingPrimary =
                RemoveExistingPrimary(
                    verticals,
                    now,
                    command.ActorUserId);

            if (demotedExistingPrimary)
            {
                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
        }

        var vertical =
            TenantCommerceVertical.Create(
                tenantId,
                command.VerticalType,
                command.IsPrimary,
                now,
                command.ActorUserId);

        await _verticalRepository.AddAsync(
            vertical,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return vertical;
    }

    private async Task<TenantCommerceVertical>
        ConfigureExistingVerticalAsync(
            Domain.Tenancy.TenantId tenantId,
            IReadOnlyCollection<TenantCommerceVertical> verticals,
            TenantCommerceVertical existing,
            ConfigureCommerceVerticalCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        if (existing.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant commerce vertical access was blocked.");
        }

        if (!command.IsEnabled)
        {
            /*
             * The domain entity itself refuses to disable a
             * primary vertical. This keeps the invariant protected
             * even when code bypasses this handler.
             */
            existing.Disable(
                now,
                command.ActorUserId);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return existing;
        }

        if (!command.IsPrimary)
        {
            if (existing.IsPrimary)
            {
                throw new CommerceConfigurationConflictException(
                    "The primary commerce vertical cannot be removed without selecting another primary vertical.");
            }

            existing.Enable(
                now,
                command.ActorUserId);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return existing;
        }

        /*
         * Already the primary: there is nothing to switch.
         * Enable is still called so this path remains correct
         * if historical data ever contains a disabled target.
         */
        if (existing.IsPrimary)
        {
            existing.Enable(
                now,
                command.ActorUserId);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return existing;
        }

        /*
         * Important ordering:
         *
         * 1. Demote current primary.
         * 2. Flush that change.
         * 3. Enable/promote the target.
         * 4. Flush again.
         *
         * A single SaveChanges is unsafe because EF Core is not
         * required to issue UPDATE statements in an order that
         * satisfies PostgreSQL's partial unique index at every
         * intermediate statement.
         */
        var demotedExistingPrimary =
            RemoveExistingPrimary(
                verticals,
                now,
                command.ActorUserId,
                excluding:
                    existing);

        if (demotedExistingPrimary)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        existing.Enable(
            now,
            command.ActorUserId);

        existing.MakePrimary(
            now,
            command.ActorUserId);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return existing;
    }

    private static bool RemoveExistingPrimary(
        IEnumerable<TenantCommerceVertical> verticals,
        DateTimeOffset now,
        Guid actorUserId,
        TenantCommerceVertical? excluding = null)
    {
        var changed =
            false;

        foreach (var vertical in verticals)
        {
            if (ReferenceEquals(
                    vertical,
                    excluding))
            {
                continue;
            }

            if (!vertical.IsPrimary)
            {
                continue;
            }

            vertical.RemovePrimary(
                now,
                actorUserId);

            changed =
                true;
        }

        return changed;
    }
}