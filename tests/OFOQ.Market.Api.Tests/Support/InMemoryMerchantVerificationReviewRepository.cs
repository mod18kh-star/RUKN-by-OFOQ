using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryMerchantVerificationReviewStore
{
    private readonly object
        _gate =
            new();

    private readonly List<MerchantVerificationProfile>
        _profiles =
            [];

    private readonly List<MerchantVerificationDocument>
        _documents =
            [];

    private readonly List<MerchantVerificationDocumentFile>
        _files =
            [];

    public void AddProfile(
        MerchantVerificationProfile profile)
    {
        lock (_gate)
        {
            _profiles.Add(
                profile);
        }
    }

    public void AddDocument(
        MerchantVerificationDocument document)
    {
        lock (_gate)
        {
            _documents.Add(
                document);
        }
    }

    public void AddFile(
        MerchantVerificationDocumentFile file)
    {
        lock (_gate)
        {
            _files.Add(
                file);
        }
    }

    public MerchantVerificationProfile?
        FindProfile(
            MerchantVerificationProfileId profileId)
    {
        lock (_gate)
        {
            return _profiles
                .SingleOrDefault(
                    profile =>
                        profile.Id ==
                        profileId);
        }
    }

    public IReadOnlyList<MerchantVerificationProfile>
        ListProfiles(
            MerchantVerificationStatus? status,
            int take)
    {
        lock (_gate)
        {
            var query =
                _profiles
                    .AsEnumerable();

            if (status.HasValue)
            {
                query =
                    query.Where(
                        profile =>
                            profile.Status ==
                            status.Value);
            }

            return query
                .OrderByDescending(
                    profile =>
                        profile.SubmittedAtUtc)
                .ThenByDescending(
                    profile =>
                        profile.CreatedAtUtc)
                .Take(
                    take)
                .ToArray();
        }
    }

    public IReadOnlyList<MerchantVerificationDocument>
        ListDocuments(
            MerchantVerificationProfileId profileId)
    {
        lock (_gate)
        {
            return _documents
                .Where(
                    document =>
                        document.ProfileId ==
                        profileId)
                .OrderBy(
                    document =>
                        document.CreatedAtUtc)
                .ToArray();
        }
    }

    public IReadOnlyList<MerchantVerificationDocumentFile>
        ListFiles(
            MerchantVerificationProfileId profileId)
    {
        lock (_gate)
        {
            var documentIds =
                _documents
                    .Where(
                        document =>
                            document.ProfileId ==
                            profileId)
                    .Select(
                        document =>
                            document.Id)
                    .ToHashSet();

            return _files
                .Where(
                    file =>
                        documentIds.Contains(
                            file.DocumentId))
                .OrderBy(
                    file =>
                        file.CreatedAtUtc)
                .ToArray();
        }
    }
}

internal sealed class InMemoryMerchantVerificationReviewRepository :
    IMerchantVerificationReviewRepository,
    IMerchantVerificationReviewQueryRepository
{
    private readonly InMemoryMerchantVerificationReviewStore
        _store;

    public InMemoryMerchantVerificationReviewRepository(
        InMemoryMerchantVerificationReviewStore store)
    {
        _store =
            store;
    }

    public Task<MerchantVerificationProfile?>
        GetProfileByIdAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.FindProfile(
                profileId));
    }

    public Task<bool>
        HasActiveTenantMembershipAsync(
            TenantId tenantId,
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        /*
         * API endpoint wiring tests do not model tenant conflict
         * scenarios; those are already covered by application tests.
         */
        return Task.FromResult(
            false);
    }

    public Task<int>
        SaveChangesAsync(
            CancellationToken cancellationToken = default)
    {
        /*
         * Domain entities are stored by reference in this fake,
         * so mutations are already visible to later reads.
         */
        return Task.FromResult(
            1);
    }

    public Task<IReadOnlyList<MerchantVerificationProfile>>
        ListProfilesAsync(
            MerchantVerificationStatus? status,
            int take,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.ListProfiles(
                status,
                take));
    }

    public Task<IReadOnlyList<MerchantVerificationDocument>>
        ListDocumentsAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.ListDocuments(
                profileId));
    }

    public Task<IReadOnlyList<MerchantVerificationDocumentFile>>
        ListDocumentFilesAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.ListFiles(
                profileId));
    }
}