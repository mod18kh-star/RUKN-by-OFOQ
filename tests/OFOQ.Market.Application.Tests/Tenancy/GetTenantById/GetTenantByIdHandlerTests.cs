using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Tenancy.GetTenantById;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Tenancy.GetTenantById;

public sealed class GetTenantByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsTenant_WhenTenantExists()
    {
        var now =
            new DateTimeOffset(
                2026,
                8,
                31,
                20,
                0,
                0,
                TimeSpan.Zero);

        var tenant =
            Tenant.Create(
                "Turks Store",
                "turks",
                now);

        var repository =
            new FakeTenantRepository(tenant);

        var handler =
            new GetTenantByIdHandler(repository);

        var result =
            await handler.HandleAsync(tenant.Id);

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.TenantId);
        Assert.Equal("Turks Store", result.Name);
        Assert.Equal("turks", result.Slug);
        Assert.Equal(TenantStatus.Draft, result.Status);
        Assert.Equal(now, result.CreatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenTenantDoesNotExist()
    {
        var repository =
            new FakeTenantRepository(null);

        var handler =
            new GetTenantByIdHandler(repository);

        var result =
            await handler.HandleAsync(
                TenantId.New());

        Assert.Null(result);
    }

    private sealed class FakeTenantRepository :
        ITenantRepository
    {
        private readonly Tenant? _tenant;

        public FakeTenantRepository(
            Tenant? tenant)
        {
            _tenant = tenant;
        }

        public Task<Tenant?> GetByIdAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            var result =
                _tenant?.Id == tenantId
                    ? _tenant
                    : null;

            return Task.FromResult(result);
        }

        public Task<Tenant?> GetBySlugAsync(
            TenantSlug slug,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Tenant?>(null);
        }

        public Task<bool> SlugExistsAsync(
            TenantSlug slug,
            TenantId? excludingTenantId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            Tenant tenant,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}