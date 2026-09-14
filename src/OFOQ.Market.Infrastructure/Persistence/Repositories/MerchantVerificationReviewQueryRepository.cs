using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class MerchantVerificationReviewQueryRepository :
    IMerchantVerificationReviewQueryRepository
{
    private readonly MarketDbContext
        _dbContext;

    public MerchantVerificationReviewQueryRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<MerchantVerificationProfile>>
        ListProfilesAsync(
            MerchantVerificationStatus? status,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext
                .MerchantVerificationProfiles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AsQueryable();

        if (status.HasValue)
        {
            query =
                query.Where(
                    profile =>
                        profile.Status ==
                        status.Value);
        }

        return await query
            .OrderByDescending(
                profile =>
                    profile.SubmittedAtUtc)
            .ThenByDescending(
                profile =>
                    profile.CreatedAtUtc)
            .Take(
                take)
            .ToListAsync(
                cancellationToken);
    }

    public Task<MerchantVerificationProfile?>
        GetProfileByIdAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        return _dbContext
            .MerchantVerificationProfiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                profile =>
                    profile.Id ==
                    profileId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<MerchantVerificationDocument>>
        ListDocumentsAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .MerchantVerificationDocuments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(
                document =>
                    document.ProfileId ==
                    profileId)
            .OrderBy(
                document =>
                    document.CreatedAtUtc)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<MerchantVerificationDocumentFile>>
        ListDocumentFilesAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        var documents =
            _dbContext
                .MerchantVerificationDocuments
                .IgnoreQueryFilters()
                .AsNoTracking();

        var files =
            _dbContext
                .MerchantVerificationDocumentFiles
                .IgnoreQueryFilters()
                .AsNoTracking();

        return await files
            .Join(
                documents,
                file =>
                    new
                    {
                        file.TenantId,
                        Id =
                            file.DocumentId
                    },
                document =>
                    new
                    {
                        document.TenantId,
                        Id =
                            document.Id
                    },
                (file, document) =>
                    new
                    {
                        File =
                            file,
                        Document =
                            document
                    })
            .Where(
                item =>
                    item.Document.ProfileId ==
                    profileId)
            .OrderBy(
                item =>
                    item.File.CreatedAtUtc)
            .Select(
                item =>
                    item.File)
            .ToListAsync(
                cancellationToken);
    }
}