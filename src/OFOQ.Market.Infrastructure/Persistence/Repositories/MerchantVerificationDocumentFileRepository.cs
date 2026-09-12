using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class MerchantVerificationDocumentFileRepository :
    IMerchantVerificationDocumentFileRepository
{
    private readonly MarketDbContext
        _dbContext;

    public MerchantVerificationDocumentFileRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<MerchantVerificationDocumentFile>> GetByDocumentAsync(
        MerchantVerificationDocumentId documentId,
        CancellationToken cancellationToken = default)
    {
        if (documentId.IsEmpty)
        {
            throw new ArgumentException(
                "Merchant verification document ID cannot be empty.",
                nameof(documentId));
        }

        return await _dbContext
            .Set<MerchantVerificationDocumentFile>()
            .Where(
                file =>
                    file.DocumentId ==
                    documentId)
            .OrderBy(
                file =>
                    file.Side)
            .ThenBy(
                file =>
                    file.CreatedAtUtc)
            .ToArrayAsync(
                cancellationToken);
    }

    public Task<MerchantVerificationDocumentFile?> GetByIdAsync(
        MerchantVerificationDocumentFileId fileId,
        CancellationToken cancellationToken = default)
    {
        if (fileId.IsEmpty)
        {
            throw new ArgumentException(
                "Merchant verification document file ID cannot be empty.",
                nameof(fileId));
        }

        return _dbContext
            .Set<MerchantVerificationDocumentFile>()
            .SingleOrDefaultAsync(
                file =>
                    file.Id ==
                    fileId,
                cancellationToken);
    }

    public Task<MerchantVerificationDocumentFile?> GetByStorageKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                storageKey))
        {
            throw new ArgumentException(
                "Private storage key is required.",
                nameof(storageKey));
        }

        var normalizedStorageKey =
            storageKey.Trim();

        return _dbContext
            .Set<MerchantVerificationDocumentFile>()
            .SingleOrDefaultAsync(
                file =>
                    file.StorageKey ==
                    normalizedStorageKey,
                cancellationToken);
    }

    public async Task AddAsync(
        MerchantVerificationDocumentFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            file);

        await _dbContext
            .Set<MerchantVerificationDocumentFile>()
            .AddAsync(
                file,
                cancellationToken);
    }

    public void Remove(
        MerchantVerificationDocumentFile file)
    {
        ArgumentNullException.ThrowIfNull(
            file);

        _dbContext
            .Set<MerchantVerificationDocumentFile>()
            .Remove(
                file);
    }
}
