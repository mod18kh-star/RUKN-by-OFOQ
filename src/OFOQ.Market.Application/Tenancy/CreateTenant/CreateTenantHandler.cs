using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.CreateTenant;

public sealed class CreateTenantHandler
{
    private readonly ITenantRepository
        _tenantRepository;

    private readonly ITenantMembershipRepository
        _membershipRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CreateTenantHandler(
        ITenantRepository tenantRepository,
        ITenantMembershipRepository membershipRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _tenantRepository =
            tenantRepository;

        _membershipRepository =
            membershipRepository;

        _userRepository =
            userRepository;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<CreateTenantResult> HandleAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        /*
         * Never create a tenant merely because a JWT existed
         * when it was issued.
         *
         * Re-read the user from persistence so suspension,
         * disablement and deletion immediately take effect.
         */
        var creator =
            await _userRepository
                .GetByIdAsync(
                    command.CreatorUserId,
                    cancellationToken);

        if (creator is null ||
            creator.IsDeleted ||
            creator.Status != UserStatus.Active)
        {
            throw new TenantCreationNotAllowedException();
        }

        var slug =
            TenantSlug.Create(
                command.Slug);

        var slugExists =
            await _tenantRepository
                .SlugExistsAsync(
                    slug,
                    excludingTenantId: null,
                    cancellationToken);

        if (slugExists)
        {
            throw new TenantSlugAlreadyExistsException(
                slug);
        }

        var now =
            _timeProvider.GetUtcNow();

        var tenant =
            Tenant.Create(
                command.Name,
                slug.Value,
                now,
                creator.Id.Value);

        /*
         * The authenticated creator automatically becomes
         * the Owner.
         *
         * Role/UserId are never accepted from the request body.
         */
        var ownerMembership =
            TenantMembership.Create(
                tenant.Id,
                creator.Id,
                TenantRole.Owner,
                now,
                creator.Id.Value);

        await _tenantRepository
            .AddAsync(
                tenant,
                cancellationToken);

        await _membershipRepository
            .AddAsync(
                ownerMembership,
                cancellationToken);

        /*
         * Tenant + Owner Membership use the same DbContext and
         * are persisted by one SaveChanges call.
         *
         * EF Core performs the database write transactionally,
         * so we never intentionally persist a tenant without
         * its initial Owner membership.
         */
        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return new CreateTenantResult(
            tenant.Id,
            tenant.Name,
            tenant.Slug.Value,
            tenant.Status);
    }
}