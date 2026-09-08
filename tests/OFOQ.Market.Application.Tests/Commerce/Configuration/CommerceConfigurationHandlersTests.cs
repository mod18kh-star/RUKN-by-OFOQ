using OFOQ.Market.Application.Commerce.Configuration.CapabilityOverrides;
using OFOQ.Market.Application.Commerce.Configuration.Common;
using OFOQ.Market.Application.Commerce.Configuration.ConfigureVertical;
using OFOQ.Market.Application.Commerce.Configuration.GetProfile;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Commerce.Configuration;

public sealed class CommerceConfigurationHandlersTests
{
    [Fact]
    public async Task Configure_FirstVerticalAsPrimary_Succeeds()
    {
        var setup =
            CreateSetup();

        var handler =
            CreateVerticalHandler(
                setup);

        var result =
            await handler.HandleAsync(
                new ConfigureCommerceVerticalCommand(
                    CommerceVerticalType.Apparel,
                    true,
                    true,
                    setup.ActorUserId));

        Assert.True(
            result.IsEnabled);

        Assert.True(
            result.IsPrimary);

        var vertical =
            Assert.Single(
                setup.Verticals.Items);

        Assert.Equal(
            CommerceVerticalType.Apparel,
            vertical.VerticalType);

        Assert.Equal(
            1,
            setup.TransactionExecutor.ExecutionCount);
    }

    [Fact]
    public async Task Configure_FirstVerticalWithoutPrimary_IsRejected()
    {
        var setup =
            CreateSetup();

        var handler =
            CreateVerticalHandler(
                setup);

        await Assert.ThrowsAsync<
            CommerceConfigurationConflictException>(
                () =>
                    handler.HandleAsync(
                        new ConfigureCommerceVerticalCommand(
                            CommerceVerticalType.Apparel,
                            true,
                            false,
                            setup.ActorUserId)));

        Assert.Equal(
            1,
            setup.TransactionExecutor.ExecutionCount);
    }

    [Fact]
    public async Task Configure_NewPrimary_DemotesOldPrimary()
    {
        var setup =
            CreateSetup();

        var handler =
            CreateVerticalHandler(
                setup);

        await handler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Apparel,
                true,
                true,
                setup.ActorUserId));

        var saveCountBeforeSwitch =
            setup.UnitOfWork.SaveCount;

        await handler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Footwear,
                true,
                true,
                setup.ActorUserId));

        var apparel =
            Assert.Single(
                setup.Verticals.Items,
                item =>
                    item.VerticalType ==
                    CommerceVerticalType.Apparel);

        var footwear =
            Assert.Single(
                setup.Verticals.Items,
                item =>
                    item.VerticalType ==
                    CommerceVerticalType.Footwear);

        Assert.False(
            apparel.IsPrimary);

        Assert.True(
            footwear.IsPrimary);

        /*
         * Switching to a new primary must flush twice:
         *
         * 1. old primary -> false
         * 2. new primary -> true
         */
        Assert.Equal(
            2,
            setup.UnitOfWork.SaveCount -
            saveCountBeforeSwitch);
    }

    [Fact]
    public async Task Configure_ExistingSecondaryAsPrimary_UsesTwoStepPersistence()
    {
        var setup =
            CreateSetup();

        var handler =
            CreateVerticalHandler(
                setup);

        await handler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Apparel,
                true,
                true,
                setup.ActorUserId));

        await handler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Footwear,
                true,
                false,
                setup.ActorUserId));

        var saveCountBeforeSwitch =
            setup.UnitOfWork.SaveCount;

        await handler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Footwear,
                true,
                true,
                setup.ActorUserId));

        var apparel =
            Assert.Single(
                setup.Verticals.Items,
                item =>
                    item.VerticalType ==
                    CommerceVerticalType.Apparel);

        var footwear =
            Assert.Single(
                setup.Verticals.Items,
                item =>
                    item.VerticalType ==
                    CommerceVerticalType.Footwear);

        Assert.False(
            apparel.IsPrimary);

        Assert.True(
            footwear.IsPrimary);

        Assert.Equal(
            2,
            setup.UnitOfWork.SaveCount -
            saveCountBeforeSwitch);
    }

    [Fact]
    public async Task Configure_PrimaryCannotBeDisabled()
    {
        var setup =
            CreateSetup();

        var handler =
            CreateVerticalHandler(
                setup);

        await handler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Apparel,
                true,
                true,
                setup.ActorUserId));

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        new ConfigureCommerceVerticalCommand(
                            CommerceVerticalType.Apparel,
                            false,
                            false,
                            setup.ActorUserId)));
    }

    [Fact]
    public async Task CapabilityOverride_DisablesDefaultCapability()
    {
        var setup =
            CreateSetup();

        var verticalHandler =
            CreateVerticalHandler(
                setup);

        await verticalHandler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Apparel,
                true,
                true,
                setup.ActorUserId));

        var overrideHandler =
            CreateOverrideHandler(
                setup);

        var profile =
            await overrideHandler.HandleAsync(
                new SetCommerceCapabilityOverrideCommand(
                    CommerceCapabilityType.MultiWarehouse,
                    false,
                    setup.ActorUserId));

        var capability =
            Assert.Single(
                profile.Capabilities,
                item =>
                    item.CapabilityType ==
                    CommerceCapabilityType.MultiWarehouse);

        Assert.False(
            capability.IsEnabled);

        Assert.True(
            capability.IsOverridden);
    }

    [Fact]
    public async Task CapabilityOverride_Null_RemovesOverrideAndRestoresDefault()
    {
        var setup =
            CreateSetup();

        var verticalHandler =
            CreateVerticalHandler(
                setup);

        await verticalHandler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Apparel,
                true,
                true,
                setup.ActorUserId));

        var overrideHandler =
            CreateOverrideHandler(
                setup);

        await overrideHandler.HandleAsync(
            new SetCommerceCapabilityOverrideCommand(
                CommerceCapabilityType.MultiWarehouse,
                false,
                setup.ActorUserId));

        var profile =
            await overrideHandler.HandleAsync(
                new SetCommerceCapabilityOverrideCommand(
                    CommerceCapabilityType.MultiWarehouse,
                    null,
                    setup.ActorUserId));

        var capability =
            Assert.Single(
                profile.Capabilities,
                item =>
                    item.CapabilityType ==
                    CommerceCapabilityType.MultiWarehouse);

        Assert.True(
            capability.IsEnabled);

        Assert.False(
            capability.IsOverridden);

        Assert.Empty(
            setup.Overrides.Items);
    }

    [Fact]
    public async Task GetProfile_MergesMultipleVerticals()
    {
        var setup =
            CreateSetup();

        var verticalHandler =
            CreateVerticalHandler(
                setup);

        await verticalHandler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Apparel,
                true,
                true,
                setup.ActorUserId));

        await verticalHandler.HandleAsync(
            new ConfigureCommerceVerticalCommand(
                CommerceVerticalType.Perfumes,
                true,
                false,
                setup.ActorUserId));

        var handler =
            new GetCommerceProfileHandler(
                setup.Verticals,
                setup.Overrides,
                setup.CurrentTenant);

        var profile =
            await handler.HandleAsync(
                new GetCommerceProfileQuery());

        Assert.Equal(
            2,
            profile.Verticals.Count);

        Assert.Contains(
            profile.Capabilities,
            capability =>
                capability.CapabilityType ==
                    CommerceCapabilityType.Bundles &&
                capability.IsEnabled);
    }

    private static TestSetup CreateSetup()
    {
        var tenantId =
            TenantId.New();

        return new TestSetup(
            tenantId,
            Guid.NewGuid(),
            new FakeCurrentTenant(
                tenantId),
            new FakeVerticalRepository(),
            new FakeOverrideRepository(),
            new FakeUnitOfWork(),
            new FakeTransactionExecutor(),
            new FixedTimeProvider(
                new DateTimeOffset(
                    2026,
                    9,
                    8,
                    12,
                    0,
                    0,
                    TimeSpan.Zero)));
    }

    private static ConfigureCommerceVerticalHandler
        CreateVerticalHandler(
            TestSetup setup)
    {
        return new ConfigureCommerceVerticalHandler(
            setup.Verticals,
            setup.CurrentTenant,
            setup.UnitOfWork,
            setup.TransactionExecutor,
            setup.TimeProvider);
    }

    private static SetCommerceCapabilityOverrideHandler
        CreateOverrideHandler(
            TestSetup setup)
    {
        return new SetCommerceCapabilityOverrideHandler(
            setup.Verticals,
            setup.Overrides,
            setup.CurrentTenant,
            setup.UnitOfWork,
            setup.TimeProvider);
    }

    private sealed record TestSetup(
        TenantId TenantId,
        Guid ActorUserId,
        FakeCurrentTenant CurrentTenant,
        FakeVerticalRepository Verticals,
        FakeOverrideRepository Overrides,
        FakeUnitOfWork UnitOfWork,
        FakeTransactionExecutor TransactionExecutor,
        FixedTimeProvider TimeProvider);

    private sealed class FakeCurrentTenant :
        ICurrentTenant
    {
        public FakeCurrentTenant(
            TenantId tenantId)
        {
            TenantId =
                tenantId;
        }

        public TenantId? TenantId { get; }

        public bool IsAvailable =>
            TenantId.HasValue &&
            !TenantId.Value.IsEmpty;
    }

    private sealed class FakeVerticalRepository :
        ITenantCommerceVerticalRepository
    {
        public List<TenantCommerceVertical> Items { get; } =
            [];

        public Task<IReadOnlyList<TenantCommerceVertical>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TenantCommerceVertical> result =
                Items.ToArray();

            return Task.FromResult(
                result);
        }

        public Task<TenantCommerceVertical?> GetByTypeAsync(
            CommerceVerticalType verticalType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item =>
                        item.VerticalType ==
                        verticalType));
        }

        public Task AddAsync(
            TenantCommerceVertical vertical,
            CancellationToken cancellationToken = default)
        {
            Items.Add(
                vertical);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeOverrideRepository :
        ITenantCommerceCapabilityOverrideRepository
    {
        public List<TenantCommerceCapabilityOverride> Items { get; } =
            [];

        public Task<IReadOnlyList<TenantCommerceCapabilityOverride>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TenantCommerceCapabilityOverride> result =
                Items.ToArray();

            return Task.FromResult(
                result);
        }

        public Task<TenantCommerceCapabilityOverride?> GetByCapabilityAsync(
            CommerceCapabilityType capabilityType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item =>
                        item.CapabilityType ==
                        capabilityType));
        }

        public Task AddAsync(
            TenantCommerceCapabilityOverride capabilityOverride,
            CancellationToken cancellationToken = default)
        {
            Items.Add(
                capabilityOverride);

            return Task.CompletedTask;
        }

        public void Remove(
            TenantCommerceCapabilityOverride capabilityOverride)
        {
            Items.Remove(
                capabilityOverride);
        }
    }

    private sealed class FakeUnitOfWork :
        IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(
                1);
        }
    }

    private sealed class FakeTransactionExecutor :
        ITransactionExecutor
    {
        public int ExecutionCount { get; private set; }

        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                operation);

            ExecutionCount++;

            return await operation(
                cancellationToken);
        }
    }

    private sealed class FixedTimeProvider :
        TimeProvider
    {
        private readonly DateTimeOffset
            _now;

        public FixedTimeProvider(
            DateTimeOffset now)
        {
            _now =
                now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}