using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class MerchantVerificationDocumentRepository :
    IMerchantVerificationDocumentRepository
{
    private readonly MarketDbContext
        _dbContext;

    public MerchantVerificationDocumentRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<MerchantVerificationDocument>> GetByProfileAsync(
        MerchantVerificationProfileId profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId.IsEmpty)
        {
            throw new ArgumentException(
                "Merchant verification profile ID cannot be empty.",
                nameof(profileId));
        }

        return await _dbContext
            .Set<MerchantVerificationDocument>()
            .Where(
                document =>
                    document.ProfileId ==
                    profileId)
            .OrderBy(
                document =>
                    document.DocumentType)
            .ThenBy(
                document =>
                    document.CreatedAtUtc)
            .ToArrayAsync(
                cancellationToken);
    }

    public Task<MerchantVerificationDocument?> GetByIdAsync(
        MerchantVerificationDocumentId documentId,
        CancellationToken cancellationToken = default)
    {
        if (documentId.IsEmpty)
        {
            throw new ArgumentException(
                "Merchant verification document ID cannot be empty.",
                nameof(documentId));
        }

        return _dbContext
            .Set<MerchantVerificationDocument>()
            .SingleOrDefaultAsync(
                document =>
                    document.Id ==
                    documentId,
                cancellationToken);
    }

    public Task<MerchantVerificationDocument?> GetByFingerprintAsync(
        MerchantVerificationProfileId profileId,
        string documentNumberFingerprint,
        CancellationToken cancellationToken = default)
    {
        if (profileId.IsEmpty)
        {
            throw new ArgumentException(
                "Merchant verification profile ID cannot be empty.",
                nameof(profileId));
        }

        if (string.IsNullOrWhiteSpace(
                documentNumberFingerprint))
        {
            throw new ArgumentException(
                "Document number fingerprint is required.",
                nameof(documentNumberFingerprint));
        }

        var normalizedFingerprint =
            documentNumberFingerprint
                .Trim()
                .ToLowerInvariant();

        return _dbContext
            .Set<MerchantVerificationDocument>()
            .SingleOrDefaultAsync(
                document =>
                    document.ProfileId ==
                        profileId &&
                    document.DocumentNumberFingerprint ==
                        normalizedFingerprint,
                cancellationToken);
    }

    public async Task AddAsync(
        MerchantVerificationDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        await _dbContext
            .Set<MerchantVerificationDocument>()
            .AddAsync(
                document,
                cancellationToken);
    }
}
