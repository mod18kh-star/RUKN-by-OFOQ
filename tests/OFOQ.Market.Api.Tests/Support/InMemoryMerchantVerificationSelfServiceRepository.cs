using System.Security.Cryptography;
using System.Text;
using OFOQ.Market.Application.Common.Files;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryMerchantVerificationSelfServiceStore
{
    private readonly object _gate = new();

    private readonly List<MerchantVerificationProfile>
        _profiles = [];

    private readonly List<MerchantVerificationDocument>
        _documents = [];

    private readonly List<MerchantVerificationDocumentFile>
        _files = [];

    public void AddProfile(
        MerchantVerificationProfile profile)
    {
        lock (_gate)
        {
            _profiles.Add(profile);
        }
    }

    public MerchantVerificationProfile?
        GetProfile(
            TenantId tenantId)
    {
        lock (_gate)
        {
            return _profiles.SingleOrDefault(
                profile =>
                    profile.TenantId == tenantId);
        }
    }

    public MerchantVerificationProfile?
        GetProfileById(
            MerchantVerificationProfileId profileId)
    {
        lock (_gate)
        {
            return _profiles.SingleOrDefault(
                profile =>
                    profile.Id == profileId);
        }
    }

    public void AddDocument(
        MerchantVerificationDocument document)
    {
        lock (_gate)
        {
            _documents.Add(document);
        }
    }

    public MerchantVerificationDocument?
        GetDocument(
            TenantId tenantId,
            MerchantVerificationDocumentId documentId)
    {
        lock (_gate)
        {
            return _documents.SingleOrDefault(
                document =>
                    document.TenantId == tenantId &&
                    document.Id == documentId);
        }
    }

    public MerchantVerificationDocument?
        GetDocumentById(
            MerchantVerificationDocumentId documentId)
    {
        lock (_gate)
        {
            return _documents.SingleOrDefault(
                document =>
                    document.Id == documentId);
        }
    }

    public MerchantVerificationDocument?
        GetDocumentByFingerprint(
            TenantId tenantId,
            MerchantVerificationProfileId profileId,
            string fingerprint)
    {
        lock (_gate)
        {
            return _documents.SingleOrDefault(
                document =>
                    document.TenantId == tenantId &&
                    document.ProfileId == profileId &&
                    document.DocumentNumberFingerprint ==
                        fingerprint);
        }
    }

    public IReadOnlyList<MerchantVerificationDocument>
        GetDocuments(
            TenantId tenantId,
            MerchantVerificationProfileId profileId)
    {
        lock (_gate)
        {
            return _documents
                .Where(
                    document =>
                        document.TenantId == tenantId &&
                        document.ProfileId == profileId)
                .ToArray();
        }
    }

    public void AddFile(
        MerchantVerificationDocumentFile file)
    {
        lock (_gate)
        {
            _files.Add(file);
        }
    }

    public MerchantVerificationDocumentFile?
        GetFile(
            TenantId tenantId,
            MerchantVerificationDocumentFileId fileId)
    {
        lock (_gate)
        {
            return _files.SingleOrDefault(
                file =>
                    file.TenantId == tenantId &&
                    file.Id == fileId);
        }
    }

    public MerchantVerificationDocumentFile?
        GetFileById(
            MerchantVerificationDocumentFileId fileId)
    {
        lock (_gate)
        {
            return _files.SingleOrDefault(
                file =>
                    file.Id == fileId);
        }
    }

    public MerchantVerificationDocumentFile?
        GetFileByStorageKey(
            TenantId tenantId,
            string storageKey)
    {
        lock (_gate)
        {
            return _files.SingleOrDefault(
                file =>
                    file.TenantId == tenantId &&
                    string.Equals(
                        file.StorageKey,
                        storageKey,
                        StringComparison.Ordinal));
        }
    }

    public IReadOnlyList<MerchantVerificationDocumentFile>
        GetFiles(
            TenantId tenantId,
            MerchantVerificationDocumentId documentId)
    {
        lock (_gate)
        {
            return _files
                .Where(
                    file =>
                        file.TenantId == tenantId &&
                        file.DocumentId == documentId)
                .ToArray();
        }
    }

    public void RemoveFile(
        MerchantVerificationDocumentFile file)
    {
        lock (_gate)
        {
            _files.Remove(file);
        }
    }
}

internal sealed class InMemoryMerchantVerificationSelfServiceRepository :
    IMerchantVerificationProfileRepository,
    IMerchantVerificationDocumentRepository,
    IMerchantVerificationDocumentFileRepository
{
    private readonly InMemoryMerchantVerificationSelfServiceStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryMerchantVerificationSelfServiceRepository(
        InMemoryMerchantVerificationSelfServiceStore store,
        ICurrentTenant currentTenant)
    {
        _store = store;
        _currentTenant = currentTenant;
    }

    public Task<MerchantVerificationProfile?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.GetProfile(
                RequireTenant()));
    }

    public Task AddAsync(
        MerchantVerificationProfile profile,
        CancellationToken cancellationToken = default)
    {
        _store.AddProfile(profile);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MerchantVerificationDocument>>
        GetByProfileAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.GetDocuments(
                RequireTenant(),
                profileId));
    }

    public Task<MerchantVerificationDocument?>
        GetByIdAsync(
            MerchantVerificationDocumentId documentId,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.GetDocument(
                RequireTenant(),
                documentId));
    }

    public Task<MerchantVerificationDocument?>
        GetByFingerprintAsync(
            MerchantVerificationProfileId profileId,
            string documentNumberFingerprint,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.GetDocumentByFingerprint(
                RequireTenant(),
                profileId,
                documentNumberFingerprint));
    }

    public Task AddAsync(
        MerchantVerificationDocument document,
        CancellationToken cancellationToken = default)
    {
        _store.AddDocument(document);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MerchantVerificationDocumentFile>>
        GetByDocumentAsync(
            MerchantVerificationDocumentId documentId,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.GetFiles(
                RequireTenant(),
                documentId));
    }

    public Task<MerchantVerificationDocumentFile?>
        GetByIdAsync(
            MerchantVerificationDocumentFileId fileId,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.GetFile(
                RequireTenant(),
                fileId));
    }

    public Task<MerchantVerificationDocumentFile?>
        GetByStorageKeyAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _store.GetFileByStorageKey(
                RequireTenant(),
                storageKey));
    }

    public Task AddAsync(
        MerchantVerificationDocumentFile file,
        CancellationToken cancellationToken = default)
    {
        _store.AddFile(file);

        return Task.CompletedTask;
    }

    public void Remove(
        MerchantVerificationDocumentFile file)
    {
        _store.RemoveFile(file);
    }

    private TenantId RequireTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new InvalidOperationException(
                "Tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}

internal sealed class FakeMerchantVerificationDocumentProtector :
    IMerchantVerificationDocumentProtector
{
    public ProtectedMerchantDocumentNumber
        ProtectDocumentNumber(
            TenantId tenantId,
            MerchantVerificationDocumentType documentType,
            string issuingCountryCode,
            string documentNumber)
    {
        return new ProtectedMerchantDocumentNumber(
            $"protected:{documentNumber}",
            ComputeDocumentNumberFingerprint(
                documentType,
                issuingCountryCode,
                documentNumber));
    }

    public string UnprotectDocumentNumber(
        TenantId tenantId,
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string protectedDocumentNumber)
    {
        return protectedDocumentNumber.StartsWith(
                "protected:",
                StringComparison.Ordinal)
            ? protectedDocumentNumber[
                "protected:".Length..]
            : protectedDocumentNumber;
    }

    public string ComputeDocumentNumberFingerprint(
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string documentNumber)
    {
        var normalized =
            $"{documentType}|{issuingCountryCode.Trim().ToUpperInvariant()}|{documentNumber.Trim()}";

        return Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        normalized)))
            .ToLowerInvariant();
    }
}

internal sealed class InMemoryMerchantVerificationPrivateFileStore :
    IMerchantVerificationPrivateFileStore
{
    private sealed record StoredFile(
        TenantId TenantId,
        byte[] Bytes,
        string ContentType);

    private readonly Dictionary<string, StoredFile>
        _files =
            new(
                StringComparer.Ordinal);

    private readonly object
        _gate =
            new();

    public async Task<MerchantVerificationPrivateFileResult>
        StoreAsync(
            TenantId tenantId,
            MerchantVerificationProfileId profileId,
            MerchantVerificationDocumentId documentId,
            MerchantVerificationDocumentSide side,
            MerchantVerificationPrivateFileUpload file,
            CancellationToken cancellationToken = default)
    {
        using var memory =
            new MemoryStream();

        await file.Content.CopyToAsync(
            memory,
            cancellationToken);

        var bytes =
            memory.ToArray();

        if (bytes.LongLength !=
            file.FileSizeBytes)
        {
            throw new InvalidOperationException(
                "Uploaded file size did not match the declared size.");
        }

        var storageKey =
            $"{tenantId.Value:N}/{profileId.Value:N}/{documentId.Value:N}/{side}/{Guid.NewGuid():N}.blob";

        lock (_gate)
        {
            _files.Add(
                storageKey,
                new StoredFile(
                    tenantId,
                    bytes,
                    file.ContentType));
        }

        return new MerchantVerificationPrivateFileResult(
            storageKey,
            Convert.ToHexString(
                    SHA256.HashData(
                        bytes))
                .ToLowerInvariant(),
            bytes.LongLength,
            file.ContentType);
    }

    public Task<Stream> OpenReadAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        StoredFile stored;

        lock (_gate)
        {
            if (!_files.TryGetValue(
                    storageKey,
                    out stored!))
            {
                throw new FileNotFoundException();
            }
        }

        if (stored.TenantId !=
            tenantId)
        {
            throw new UnauthorizedAccessException();
        }

        Stream result =
            new MemoryStream(
                stored.Bytes,
                writable:
                    false);

        return Task.FromResult(
            result);
    }

    public Task DeleteAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_files.TryGetValue(
                    storageKey,
                    out var stored) &&
                stored.TenantId ==
                    tenantId)
            {
                _files.Remove(
                    storageKey);
            }
        }

        return Task.CompletedTask;
    }
}