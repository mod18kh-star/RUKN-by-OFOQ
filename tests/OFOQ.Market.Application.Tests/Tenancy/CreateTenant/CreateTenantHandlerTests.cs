using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Tenancy.CreateTenant;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Tenancy.CreateTenant;

public sealed class CreateTenantHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithAvailableSlug_CreatesTenantAndOwnerMembership()
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

        var creator =
            User.Create(
                "owner@example.com",
                "HASHED-PASSWORD",
                now);

        var tenantRepository =
            new FakeTenantRepository();

        var membershipRepository =
            new CapturingTenantMembershipRepository();

        var userRepository =
            new SingleUserRepository(
                creator);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateTenantHandler(
                tenantRepository,
                membershipRepository,
                userRepository,
                unitOfWork,
                new FixedTimeProvider(
                    now));

        var result =
            await handler.HandleAsync(
                new CreateTenantCommand(
                    "Turks Store",
                    "turks",
                    creator.Id));

        Assert.NotNull(
            tenantRepository.AddedTenant);

        var tenant =
            tenantRepository.AddedTenant;

        Assert.Equal(
            "Turks Store",
            tenant.Name);

        Assert.Equal(
            "turks",
            tenant.Slug.Value);

        Assert.Equal(
            TenantStatus.Draft,
            tenant.Status);

        Assert.Equal(
            now,
            tenant.CreatedAtUtc);

        Assert.Equal(
            creator.Id.Value,
            tenant.CreatedByUserId);

        Assert.Equal(
            tenant.Id,
            result.TenantId);

        Assert.NotNull(
            membershipRepository.AddedMembership);

        var membership =
            membershipRepository.AddedMembership;

        Assert.Equal(
            tenant.Id,
            membership.TenantId);

        Assert.Equal(
            creator.Id,
            membership.UserId);

        Assert.Equal(
            TenantRole.Owner,
            membership.Role);

        Assert.Equal(
            now,
            membership.CreatedAtUtc);

        Assert.Equal(
            creator.Id.Value,
            membership.CreatedByUserId);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_NormalizesSlugBeforeCreatingTenant()
    {
        var creator =
            User.Create(
                "owner@example.com",
                "HASHED-PASSWORD",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            new FakeTenantRepository();

        var handler =
            new CreateTenantHandler(
                tenantRepository,
                new CapturingTenantMembershipRepository(),
                new SingleUserRepository(
                    creator),
                new FakeUnitOfWork(),
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        var result =
            await handler.HandleAsync(
                new CreateTenantCommand(
                    "Turks Store",
                    "TURKS",
                    creator.Id));

        Assert.Equal(
            "turks",
            result.Slug);

        Assert.Equal(
            "turks",
            tenantRepository
                .AddedTenant!
                .Slug
                .Value);
    }

    [Fact]
    public async Task HandleAsync_WithExistingSlug_ThrowsTenantSlugAlreadyExistsException()
    {
        var creator =
            User.Create(
                "owner@example.com",
                "HASHED-PASSWORD",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            new FakeTenantRepository
            {
                SlugExists =
                    true
            };

        var membershipRepository =
            new CapturingTenantMembershipRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateTenantHandler(
                tenantRepository,
                membershipRepository,
                new SingleUserRepository(
                    creator),
                unitOfWork,
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        var exception =
            await Assert.ThrowsAsync<
                TenantSlugAlreadyExistsException>(
                () =>
                    handler.HandleAsync(
                        new CreateTenantCommand(
                            "Turks Store",
                            "turks",
                            creator.Id)));

        Assert.Equal(
            "turks",
            exception.Slug.Value);

        Assert.Null(
            tenantRepository.AddedTenant);

        Assert.Null(
            membershipRepository.AddedMembership);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidSlug_DoesNotPersistAnything()
    {
        var creator =
            User.Create(
                "owner@example.com",
                "HASHED-PASSWORD",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            new FakeTenantRepository();

        var membershipRepository =
            new CapturingTenantMembershipRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateTenantHandler(
                tenantRepository,
                membershipRepository,
                new SingleUserRepository(
                    creator),
                unitOfWork,
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<
            ArgumentException>(
                () =>
                    handler.HandleAsync(
                        new CreateTenantCommand(
                            "Turks Store",
                            "turks store",
                            creator.Id)));

        Assert.Null(
            tenantRepository.AddedTenant);

        Assert.Null(
            membershipRepository.AddedMembership);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatorDoesNotExist_IsDenied()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var membershipRepository =
            new CapturingTenantMembershipRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateTenantHandler(
                tenantRepository,
                membershipRepository,
                new SingleUserRepository(
                    user: null),
                unitOfWork,
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<
            TenantCreationNotAllowedException>(
                () =>
                    handler.HandleAsync(
                        new CreateTenantCommand(
                            "Turks Store",
                            "turks",
                            UserId.New())));

        Assert.Null(
            tenantRepository.AddedTenant);

        Assert.Null(
            membershipRepository.AddedMembership);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatorIsSuspended_IsDenied()
    {
        var now =
            DateTimeOffset.UtcNow;

        var creator =
            User.Create(
                "owner@example.com",
                "HASHED-PASSWORD",
                now);

        creator.Suspend(
            now.AddMinutes(1));

        var tenantRepository =
            new FakeTenantRepository();

        var membershipRepository =
            new CapturingTenantMembershipRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateTenantHandler(
                tenantRepository,
                membershipRepository,
                new SingleUserRepository(
                    creator),
                unitOfWork,
                new FixedTimeProvider(
                    now.AddMinutes(2)));

        await Assert.ThrowsAsync<
            TenantCreationNotAllowedException>(
                () =>
                    handler.HandleAsync(
                        new CreateTenantCommand(
                            "Turks Store",
                            "turks",
                            creator.Id)));

        Assert.Null(
            tenantRepository.AddedTenant);

        Assert.Null(
            membershipRepository.AddedMembership);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    private sealed class SingleUserRepository :
        IUserRepository
    {
        private User? _user;

        public SingleUserRepository(
            User? user)
        {
            _user =
                user;
        }

        public Task<User?> GetByIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _user?.Id == userId
                    ? _user
                    : null);
        }

        public Task<User?> GetByEmailAsync(
            EmailAddress email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _user?.Email == email
                    ? _user
                    : null);
        }

        public Task<bool> EmailExistsAsync(
            EmailAddress email,
            UserId? excludingUserId = null,
            CancellationToken cancellationToken = default)
        {
            var exists =
                _user is not null &&
                _user.Email == email &&
                (!excludingUserId.HasValue ||
                 _user.Id != excludingUserId.Value);

            return Task.FromResult(
                exists);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            _user =
                user;

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingTenantMembershipRepository :
        ITenantMembershipRepository
    {
        public TenantMembership? AddedMembership
        {
            get;
            private set;
        }

        public Task<TenantMembership?> GetByIdAsync(
            TenantMembershipId membershipId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AddedMembership?.Id ==
                membershipId
                    ? AddedMembership
                    : null);
        }

        public Task<TenantMembership?> GetByTenantAndUserAsync(
            TenantId tenantId,
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AddedMembership is not null &&
                AddedMembership.TenantId ==
                    tenantId &&
                AddedMembership.UserId ==
                    userId
                    ? AddedMembership
                    : null);
        }

        public Task<IReadOnlyList<TenantMembership>>
            GetByTenantIdAsync(
                TenantId tenantId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TenantMembership> result =
                AddedMembership is not null &&
                AddedMembership.TenantId ==
                    tenantId
                    ? new[]
                    {
                        AddedMembership
                    }
                    : Array.Empty<TenantMembership>();

            return Task.FromResult(
                result);
        }

        public Task<IReadOnlyList<TenantMembership>>
            GetByUserIdAsync(
                UserId userId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TenantMembership> result =
                AddedMembership is not null &&
                AddedMembership.UserId ==
                    userId
                    ? new[]
                    {
                        AddedMembership
                    }
                    : Array.Empty<TenantMembership>();

            return Task.FromResult(
                result);
        }

        public Task<bool> ExistsAsync(
            TenantId tenantId,
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AddedMembership is not null &&
                AddedMembership.TenantId ==
                    tenantId &&
                AddedMembership.UserId ==
                    userId);
        }

        public Task AddAsync(
            TenantMembership membership,
            CancellationToken cancellationToken = default)
        {
            AddedMembership =
                membership;

            return Task.CompletedTask;
        }
    }
}