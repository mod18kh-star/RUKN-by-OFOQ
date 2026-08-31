using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.CreateTenant;

public sealed class CreateTenantHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateTenantHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<CreateTenantResult> HandleAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var slug =
            TenantSlug.Create(command.Slug);

        var slugExists =
            await _tenantRepository.SlugExistsAsync(
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
                command.CreatedByUserId);

        await _tenantRepository.AddAsync(
            tenant,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new CreateTenantResult(
            tenant.Id,
            tenant.Name,
            tenant.Slug.Value,
            tenant.Status);
    }
}