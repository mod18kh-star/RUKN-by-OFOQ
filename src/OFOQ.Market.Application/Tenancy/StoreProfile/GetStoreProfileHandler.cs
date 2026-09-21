using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Tenancy.StoreProfile;

public sealed class GetStoreProfileHandler
{
    private readonly ICurrentTenant
        _currentTenant;

    private readonly ITenantRepository
        _tenantRepository;

    private readonly ITenantStoreProfileRepository
        _profileRepository;

    private readonly ITenantStoreSocialLinkRepository
        _socialLinkRepository;

    public GetStoreProfileHandler(
        ICurrentTenant currentTenant,
        ITenantRepository tenantRepository,
        ITenantStoreProfileRepository profileRepository,
        ITenantStoreSocialLinkRepository socialLinkRepository)
    {
        _currentTenant =
            currentTenant;

        _tenantRepository =
            tenantRepository;

        _profileRepository =
            profileRepository;

        _socialLinkRepository =
            socialLinkRepository;
    }

    public async Task<StoreProfileResult?> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "Tenant context is required.");
        }

        var tenantId =
            _currentTenant.TenantId.Value;

        var tenant =
            await _tenantRepository
                .GetByIdAsync(
                    tenantId,
                    cancellationToken);

        if (tenant is null ||
            tenant.IsDeleted)
        {
            return null;
        }

        var profile =
            await _profileRepository
                .GetAsync(
                    cancellationToken);

        var socialLinks =
            await _socialLinkRepository
                .GetAllAsync(
                    cancellationToken);

        return new StoreProfileResult(
            tenant.Id.Value,
            tenant.Name,
            tenant.Slug.Value,
            tenant.Status.ToString(),
            profile?.WebsiteUrl,
            profile?.WhatsAppNumber,
            profile?.CustomerServicePhone,
            profile?.SecondaryPhone,
            profile?.LandlinePhone,
            profile?.PhysicalAddress,
            profile?.GoogleMapsUrl,
            profile?.CommercialRegistrationNumber,
            profile?.CommercialRegistrationNotApplicable ?? false,
            profile?.ShowWebsite ?? false,
            profile?.ShowWhatsApp ?? false,
            profile?.ShowCustomerServicePhone ?? false,
            profile?.ShowSecondaryPhone ?? false,
            profile?.ShowLandlinePhone ?? false,
            profile?.ShowPhysicalAddress ?? false,
            profile?.ShowCommercialRegistration ?? false,
            socialLinks
                .OrderBy(
                    item =>
                        item.SortOrder)
                .ThenBy(
                    item =>
                        item.PlatformCode)
                .Select(
                    item =>
                        new StoreSocialLinkResult(
                            item.Id.Value,
                            item.PlatformCode,
                            item.Label,
                            item.Url,
                            item.SortOrder,
                            item.IsVisible))
                .ToArray());
    }
}
