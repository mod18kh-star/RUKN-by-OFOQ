using OFOQ.Market.Application.Common.Files;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Commerce.Verification.Merchant;

public sealed record MerchantVerificationFileResult(
    MerchantVerificationDocumentFileId FileId,
    MerchantVerificationDocumentSide Side,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record MerchantVerificationDocumentResult(
    MerchantVerificationDocumentId DocumentId,
    MerchantVerificationDocumentType DocumentType,
    string IssuingCountryCode,
    string HolderName,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    MerchantVerificationDocumentReviewStatus ReviewStatus,
    string? ReviewNote,
    DateTimeOffset? ReviewedAtUtc,
    IReadOnlyList<MerchantVerificationFileResult> Files);

public sealed record MerchantVerificationProfileResult(
    MerchantVerificationProfileId ProfileId,
    TenantId TenantId,
    UserId PrincipalUserId,
    MerchantVerificationSubjectType SubjectType,
    string CountryCode,
    string LegalName,
    MerchantVerificationStatus Status,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ReviewStartedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset? VerifiedAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    string? ReviewNote);

public sealed record MerchantVerificationSelfServiceResult(
    MerchantVerificationProfileResult? Profile,
    IReadOnlyList<MerchantVerificationDocumentResult> Documents);

public sealed record UpsertMerchantVerificationProfileCommand(
    UserId ActorUserId,
    MerchantVerificationSubjectType SubjectType,
    string CountryCode,
    string LegalName);

public sealed record UpsertMerchantVerificationDocumentCommand(
    MerchantVerificationDocumentId? DocumentId,
    UserId ActorUserId,
    MerchantVerificationDocumentType DocumentType,
    string IssuingCountryCode,
    string HolderName,
    string DocumentNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate);

public sealed record UploadMerchantVerificationFileCommand(
    MerchantVerificationDocumentId DocumentId,
    MerchantVerificationDocumentSide Side,
    UserId ActorUserId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content);

public sealed record MerchantVerificationPrivateFileDownload(
    string OriginalFileName,
    string ContentType,
    Stream Content);

internal static class MerchantVerificationMerchantGuard
{
    public static void EnsureEditable(
        MerchantVerificationProfile profile)
    {
        if (profile.Status is
            MerchantVerificationStatus.Submitted or
            MerchantVerificationStatus.UnderReview or
            MerchantVerificationStatus.Verified)
        {
            throw new InvalidOperationException(
                "Merchant verification cannot be edited while submitted, under review, or verified.");
        }
    }
}

public sealed class GetMerchantVerificationSelfServiceHandler
{
    private readonly IMerchantVerificationProfileRepository
        _profileRepository;

    private readonly IMerchantVerificationDocumentRepository
        _documentRepository;

    private readonly IMerchantVerificationDocumentFileRepository
        _fileRepository;

    public GetMerchantVerificationSelfServiceHandler(
        IMerchantVerificationProfileRepository profileRepository,
        IMerchantVerificationDocumentRepository documentRepository,
        IMerchantVerificationDocumentFileRepository fileRepository)
    {
        _profileRepository =
            profileRepository;

        _documentRepository =
            documentRepository;

        _fileRepository =
            fileRepository;
    }

    public async Task<MerchantVerificationSelfServiceResult>
        HandleAsync(
            CancellationToken cancellationToken = default)
    {
        var profile =
            await _profileRepository.GetAsync(
                cancellationToken);

        if (profile is null)
        {
            return new MerchantVerificationSelfServiceResult(
                null,
                []);
        }

        var documents =
            await _documentRepository.GetByProfileAsync(
                profile.Id,
                cancellationToken);

        var results =
            new List<MerchantVerificationDocumentResult>();

        foreach (var document in documents)
        {
            var files =
                await _fileRepository.GetByDocumentAsync(
                    document.Id,
                    cancellationToken);

            results.Add(
                MapDocument(
                    document,
                    files));
        }

        return new MerchantVerificationSelfServiceResult(
            MapProfile(
                profile),
            results);
    }

    internal static MerchantVerificationProfileResult
        MapProfile(
            MerchantVerificationProfile profile)
    {
        return new MerchantVerificationProfileResult(
            profile.Id,
            profile.TenantId,
            profile.PrincipalUserId,
            profile.SubjectType,
            profile.CountryCode,
            profile.LegalName,
            profile.Status,
            profile.SubmittedAtUtc,
            profile.ReviewStartedAtUtc,
            profile.ReviewedAtUtc,
            profile.VerifiedAtUtc,
            profile.ExpiredAtUtc,
            profile.ReviewNote);
    }

    internal static MerchantVerificationDocumentResult
        MapDocument(
            MerchantVerificationDocument document,
            IReadOnlyList<MerchantVerificationDocumentFile> files)
    {
        return new MerchantVerificationDocumentResult(
            document.Id,
            document.DocumentType,
            document.IssuingCountryCode,
            document.HolderName,
            document.IssueDate,
            document.ExpiryDate,
            document.ReviewStatus,
            document.ReviewNote,
            document.ReviewedAtUtc,
            files
                .Select(
                    file =>
                        new MerchantVerificationFileResult(
                            file.Id,
                            file.Side,
                            file.OriginalFileName,
                            file.ContentType,
                            file.FileSizeBytes,
                            file.CreatedAtUtc))
                .ToArray());
    }
}

public sealed class UpsertMerchantVerificationProfileHandler
{
    private readonly ICurrentTenant
        _currentTenant;

    private readonly IMerchantVerificationProfileRepository
        _repository;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public UpsertMerchantVerificationProfileHandler(
        ICurrentTenant currentTenant,
        IMerchantVerificationProfileRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _currentTenant =
            currentTenant;

        _repository =
            repository;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<MerchantVerificationProfileResult>
        HandleAsync(
            UpsertMerchantVerificationProfileCommand command,
            CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new InvalidOperationException(
                "Tenant context is required.");
        }

        if (command.ActorUserId.IsEmpty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(command));
        }

        var now =
            _timeProvider.GetUtcNow();

        var profile =
            await _repository.GetAsync(
                cancellationToken);

        if (profile is null)
        {
            profile =
                MerchantVerificationProfile.Create(
                    _currentTenant.TenantId.Value,
                    command.ActorUserId,
                    command.SubjectType,
                    command.CountryCode,
                    command.LegalName,
                    now,
                    command.ActorUserId.Value);

            await _repository.AddAsync(
                profile,
                cancellationToken);
        }
        else
        {
            profile.UpdateDetails(
                command.SubjectType,
                command.CountryCode,
                command.LegalName,
                now,
                command.ActorUserId.Value);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return GetMerchantVerificationSelfServiceHandler.MapProfile(
            profile);
    }
}

public sealed class UpsertMerchantVerificationDocumentHandler
{
    private readonly IMerchantVerificationProfileRepository
        _profileRepository;

    private readonly IMerchantVerificationDocumentRepository
        _documentRepository;

    private readonly IMerchantVerificationDocumentProtector
        _protector;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public UpsertMerchantVerificationDocumentHandler(
        IMerchantVerificationProfileRepository profileRepository,
        IMerchantVerificationDocumentRepository documentRepository,
        IMerchantVerificationDocumentProtector protector,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _profileRepository =
            profileRepository;

        _documentRepository =
            documentRepository;

        _protector =
            protector;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<MerchantVerificationDocumentResult>
        HandleAsync(
            UpsertMerchantVerificationDocumentCommand command,
            CancellationToken cancellationToken = default)
    {
        var profile =
            await _profileRepository.GetAsync(
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Merchant verification profile was not found.");

        MerchantVerificationMerchantGuard.EnsureEditable(
            profile);

        var protectedNumber =
            _protector.ProtectDocumentNumber(
                profile.TenantId,
                command.DocumentType,
                command.IssuingCountryCode,
                command.DocumentNumber);

        var duplicate =
            await _documentRepository.GetByFingerprintAsync(
                profile.Id,
                protectedNumber.Fingerprint,
                cancellationToken);

        MerchantVerificationDocument document;

        if (!command.DocumentId.HasValue)
        {
            if (duplicate is not null)
            {
                throw new InvalidOperationException(
                    "This verification document already exists.");
            }

            document =
                MerchantVerificationDocument.Create(
                    profile.TenantId,
                    profile.Id,
                    command.DocumentType,
                    command.IssuingCountryCode,
                    command.HolderName,
                    protectedNumber.ProtectedValue,
                    protectedNumber.Fingerprint,
                    command.IssueDate,
                    command.ExpiryDate,
                    _timeProvider.GetUtcNow(),
                    command.ActorUserId.Value);

            await _documentRepository.AddAsync(
                document,
                cancellationToken);
        }
        else
        {
            document =
                await _documentRepository.GetByIdAsync(
                    command.DocumentId.Value,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    "Merchant verification document was not found.");

            if (document.ProfileId !=
                profile.Id)
            {
                throw new KeyNotFoundException(
                    "Merchant verification document was not found.");
            }

            if (duplicate is not null &&
                duplicate.Id != document.Id)
            {
                throw new InvalidOperationException(
                    "This verification document already exists.");
            }

            document.UpdateDetails(
                command.DocumentType,
                command.IssuingCountryCode,
                command.HolderName,
                protectedNumber.ProtectedValue,
                protectedNumber.Fingerprint,
                command.IssueDate,
                command.ExpiryDate,
                _timeProvider.GetUtcNow(),
                command.ActorUserId.Value);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return GetMerchantVerificationSelfServiceHandler.MapDocument(
            document,
            []);
    }
}

public sealed class UploadMerchantVerificationFileHandler
{
    private static readonly HashSet<string>
        AllowedContentTypes =
            new(
                [
                    "image/jpeg",
                    "image/png",
                    "image/webp",
                    "application/pdf"
                ],
                StringComparer.OrdinalIgnoreCase);

    private readonly IMerchantVerificationProfileRepository
        _profileRepository;

    private readonly IMerchantVerificationDocumentRepository
        _documentRepository;

    private readonly IMerchantVerificationDocumentFileRepository
        _fileRepository;

    private readonly IMerchantVerificationPrivateFileStore
        _fileStore;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public UploadMerchantVerificationFileHandler(
        IMerchantVerificationProfileRepository profileRepository,
        IMerchantVerificationDocumentRepository documentRepository,
        IMerchantVerificationDocumentFileRepository fileRepository,
        IMerchantVerificationPrivateFileStore fileStore,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _profileRepository =
            profileRepository;

        _documentRepository =
            documentRepository;

        _fileRepository =
            fileRepository;

        _fileStore =
            fileStore;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<MerchantVerificationFileResult>
        HandleAsync(
            UploadMerchantVerificationFileCommand command,
            CancellationToken cancellationToken = default)
    {
        if (command.Side ==
                MerchantVerificationDocumentSide.Unknown ||
            !Enum.IsDefined(
                command.Side))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "A supported document side is required.");
        }

        if (command.FileSizeBytes <= 0 ||
            command.FileSizeBytes >
                MerchantVerificationDocumentFile.MaxFileSizeBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                $"File size must be between 1 and {MerchantVerificationDocumentFile.MaxFileSizeBytes} bytes.");
        }

        if (!AllowedContentTypes.Contains(
                command.ContentType))
        {
            throw new ArgumentException(
                "Only JPEG, PNG, WebP, and PDF verification files are supported.",
                nameof(command));
        }

        var profile =
            await _profileRepository.GetAsync(
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Merchant verification profile was not found.");

        MerchantVerificationMerchantGuard.EnsureEditable(
            profile);

        var document =
            await _documentRepository.GetByIdAsync(
                command.DocumentId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Merchant verification document was not found.");

        if (document.ProfileId !=
            profile.Id)
        {
            throw new KeyNotFoundException(
                "Merchant verification document was not found.");
        }

        var previousFiles =
            await _fileRepository.GetByDocumentAsync(
                document.Id,
                cancellationToken);

        var previousSideFiles =
            previousFiles
                .Where(
                    file =>
                        file.Side ==
                        command.Side)
                .ToArray();

        MerchantVerificationPrivateFileResult?
            stored =
                null;

        try
        {
            stored =
                await _fileStore.StoreAsync(
                    profile.TenantId,
                    profile.Id,
                    document.Id,
                    command.Side,
                    new MerchantVerificationPrivateFileUpload(
                        command.OriginalFileName,
                        command.ContentType,
                        command.FileSizeBytes,
                        command.Content),
                    cancellationToken);

            var entity =
                MerchantVerificationDocumentFile.Create(
                    profile.TenantId,
                    document.Id,
                    command.Side,
                    stored.StorageKey,
                    command.OriginalFileName,
                    stored.ContentType,
                    stored.FileSizeBytes,
                    stored.Sha256,
                    _timeProvider.GetUtcNow(),
                    command.ActorUserId.Value);

            await _fileRepository.AddAsync(
                entity,
                cancellationToken);

            foreach (var previousFile in previousSideFiles)
            {
                _fileRepository.Remove(
                    previousFile);
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            foreach (var previousFile in previousSideFiles)
            {
                try
                {
                    await _fileStore.DeleteAsync(
                        previousFile.TenantId,
                        previousFile.StorageKey,
                        cancellationToken);
                }
                catch
                {
                    /*
                     * The database already points at the new private
                     * object. Old orphan cleanup can be retried later.
                     */
                }
            }

            return new MerchantVerificationFileResult(
                entity.Id,
                entity.Side,
                entity.OriginalFileName,
                entity.ContentType,
                entity.FileSizeBytes,
                entity.CreatedAtUtc);
        }
        catch
        {
            if (stored is not null)
            {
                try
                {
                    await _fileStore.DeleteAsync(
                        profile.TenantId,
                        stored.StorageKey,
                        CancellationToken.None);
                }
                catch
                {
                }
            }

            throw;
        }
    }
}

public sealed class SubmitMerchantVerificationHandler
{
    private readonly IMerchantVerificationProfileRepository
        _profileRepository;

    private readonly IMerchantVerificationDocumentRepository
        _documentRepository;

    private readonly IMerchantVerificationDocumentFileRepository
        _fileRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public SubmitMerchantVerificationHandler(
        IMerchantVerificationProfileRepository profileRepository,
        IMerchantVerificationDocumentRepository documentRepository,
        IMerchantVerificationDocumentFileRepository fileRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _profileRepository =
            profileRepository;

        _documentRepository =
            documentRepository;

        _fileRepository =
            fileRepository;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<MerchantVerificationProfileResult>
        HandleAsync(
            UserId actorUserId,
            CancellationToken cancellationToken = default)
    {
        var profile =
            await _profileRepository.GetAsync(
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Merchant verification profile was not found.");

        var documents =
            await _documentRepository.GetByProfileAsync(
                profile.Id,
                cancellationToken);

        if (documents.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one verification document is required before submission.");
        }

        foreach (var document in documents)
        {
            var files =
                await _fileRepository.GetByDocumentAsync(
                    document.Id,
                    cancellationToken);

            if (files.Count == 0)
            {
                throw new InvalidOperationException(
                    "Every verification document must contain at least one private file before submission.");
            }
        }

        profile.Submit(
            _timeProvider.GetUtcNow(),
            actorUserId.Value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return GetMerchantVerificationSelfServiceHandler.MapProfile(
            profile);
    }
}

public sealed class OpenMerchantVerificationOwnFileHandler
{
    private readonly IMerchantVerificationDocumentFileRepository
        _repository;

    private readonly IMerchantVerificationPrivateFileStore
        _fileStore;

    public OpenMerchantVerificationOwnFileHandler(
        IMerchantVerificationDocumentFileRepository repository,
        IMerchantVerificationPrivateFileStore fileStore)
    {
        _repository =
            repository;

        _fileStore =
            fileStore;
    }

    public async Task<MerchantVerificationPrivateFileDownload?>
        HandleAsync(
            MerchantVerificationDocumentFileId fileId,
            CancellationToken cancellationToken = default)
    {
        var file =
            await _repository.GetByIdAsync(
                fileId,
                cancellationToken);

        if (file is null)
        {
            return null;
        }

        var stream =
            await _fileStore.OpenReadAsync(
                file.TenantId,
                file.StorageKey,
                cancellationToken);

        return new MerchantVerificationPrivateFileDownload(
            file.OriginalFileName,
            file.ContentType,
            stream);
    }
}