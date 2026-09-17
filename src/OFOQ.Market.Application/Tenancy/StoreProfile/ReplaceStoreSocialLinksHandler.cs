using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.StoreProfile;

public sealed class ReplaceStoreSocialLinksHandler
{
    public const int MaxSocialLinks =
        12;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly ITenantStoreSocialLinkRepository
        _socialLinkRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public ReplaceStoreSocialLinksHandler(
        ICurrentTenant currentTenant,
        ITenantStoreSocialLinkRepository socialLinkRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _currentTenant =
            currentTenant;

        _socialLinkRepository =
            socialLinkRepository;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task HandleAsync(
        ReplaceStoreSocialLinksCommand command,
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

        if (command.SocialLinks.Count >
            MaxSocialLinks)
        {
            throw new ArgumentException(
                $"A store can expose at most {MaxSocialLinks} social links.",
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

        var now =
            _timeProvider.GetUtcNow();

        var replacements =
            command.SocialLinks
                .Select(
                    (item, index) =>
                        TenantStoreSocialLink.Create(
                            tenantId,
                            item.PlatformCode,
                            item.Label,
                            item.Url,
                            index,
                            item.IsVisible,
                            now,
                            command.ActorUserId))
                .ToArray();

        var existing =
            await _socialLinkRepository
                .GetAllAsync(
                    cancellationToken);

        if (existing.Any(
                item =>
                    item.TenantId !=
                    tenantId))
        {
            throw new TenantScopeViolationException(
                "Cross-tenant store social-link access was blocked.");
        }

        _socialLinkRepository
            .RemoveRange(
                existing);

        await _socialLinkRepository
            .AddRangeAsync(
                replacements,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);
    }
}
