using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.StoreProfile;

public sealed class UpdateStoreProfileHandler
{
    private readonly ICurrentTenant
        _currentTenant;

    private readonly ITenantStoreProfileRepository
        _profileRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public UpdateStoreProfileHandler(
        ICurrentTenant currentTenant,
        ITenantStoreProfileRepository profileRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _currentTenant =
            currentTenant;

        _profileRepository =
            profileRepository;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task HandleAsync(
        UpdateStoreProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (command.ActorUserId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(command));
        }

        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "Tenant context is required.");
        }

        var tenantId =
            _currentTenant.TenantId.Value;

        var profile =
            await _profileRepository
                .GetAsync(
                    cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        if (profile is null)
        {
            profile =
                TenantStoreProfile.Create(
                    tenantId,
                    command.WebsiteUrl,
                    command.WhatsAppNumber,
                    command.CustomerServicePhone,
                    command.SecondaryPhone,
                    command.LandlinePhone,
                    command.PhysicalAddress,
                    command.GoogleMapsUrl,
                    command.CommercialRegistrationNumber,
                    command.CommercialRegistrationNotApplicable,
                    command.ShowWebsite,
                    command.ShowWhatsApp,
                    command.ShowCustomerServicePhone,
                    command.ShowSecondaryPhone,
                    command.ShowLandlinePhone,
                    command.ShowPhysicalAddress,
                    command.ShowCommercialRegistration,
                    now,
                    command.ActorUserId);

            await _profileRepository
                .AddAsync(
                    profile,
                    cancellationToken);
        }
        else
        {
            if (profile.TenantId !=
                tenantId)
            {
                throw new TenantScopeViolationException(
                    "Cross-tenant store-profile access was blocked.");
            }

            profile.Update(
                command.WebsiteUrl,
                command.WhatsAppNumber,
                command.CustomerServicePhone,
                command.SecondaryPhone,
                command.LandlinePhone,
                command.PhysicalAddress,
                command.GoogleMapsUrl,
                command.CommercialRegistrationNumber,
                command.CommercialRegistrationNotApplicable,
                command.ShowWebsite,
                command.ShowWhatsApp,
                command.ShowCustomerServicePhone,
                command.ShowSecondaryPhone,
                command.ShowLandlinePhone,
                command.ShowPhysicalAddress,
                command.ShowCommercialRegistration,
                now,
                command.ActorUserId);
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);
    }
}
