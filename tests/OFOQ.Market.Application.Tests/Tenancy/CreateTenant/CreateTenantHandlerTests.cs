using OFOQ.Market.Application.Tenancy.CreateTenant;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Tenancy.CreateTenant;

public sealed class CreateTenantHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithAvailableSlug_CreatesTenant()
    {
        var now = new DateTimeOffset(
            2026,
            8,
            31,
            20,
            0,
            0,
            TimeSpan.Zero);

        var repository =
            new FakeTenantRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var timeProvider =
            new FixedTimeProvider(now);

        var handler =
            new CreateTenantHandler(
                repository,
                unitOfWork,
                timeProvider);

        var command =
            new CreateTenantCommand(
                "Turks Store",
                "turks");

        var result =
            await handler.HandleAsync(command);

        Assert.NotNull(repository.AddedTenant);

        Assert.Equal(
            "Turks Store",
            repository.AddedTenant.Name);

        Assert.Equal(
            "turks",
            repository.AddedTenant.Slug.Value);

        Assert.Equal(
            TenantStatus.Draft,
            repository.AddedTenant.Status);

        Assert.Equal(
            now,
            repository.AddedTenant.CreatedAtUtc);

        Assert.Equal(
            repository.AddedTenant.Id,
            result.TenantId);

        Assert.Equal(
            "Turks Store",
            result.Name);

        Assert.Equal(
            "turks",
            result.Slug);

        Assert.Equal(
            TenantStatus.Draft,
            result.Status);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_NormalizesSlugBeforeCheckingAvailability()
    {
        var repository =
            new FakeTenantRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateTenantHandler(
                repository,
                unitOfWork,
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        var result =
            await handler.HandleAsync(
                new CreateTenantCommand(
                    "Turks Store",
                    "TURKS"));

        Assert.Equal(
            "turks",
            result.Slug);

        Assert.Equal(
            "turks",
            repository.AddedTenant!.Slug.Value);
    }

    [Fact]
    public async Task HandleAsync_WithExistingSlug_ThrowsTenantSlugAlreadyExistsException()
    {
        var repository =
            new FakeTenantRepository
            {
                SlugExists = true
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateTenantHandler(
                repository,
                unitOfWork,
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        var exception =
            await Assert.ThrowsAsync<
                TenantSlugAlreadyExistsException>(
                () => handler.HandleAsync(
                    new CreateTenantCommand(
                        "Turks Store",
                        "turks")));

        Assert.Equal(
            "turks",
            exception.Slug.Value);

        Assert.Null(
            repository.AddedTenant);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidSlug_DoesNotPersistAnything()
    {
        var repository =
            new FakeTenantRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateTenantHandler(
                repository,
                unitOfWork,
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                new CreateTenantCommand(
                    "Turks Store",
                    "turks store")));

        Assert.Null(
            repository.AddedTenant);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
}