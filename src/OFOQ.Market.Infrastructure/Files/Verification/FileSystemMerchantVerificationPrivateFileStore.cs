using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using OFOQ.Market.Application.Common.Files;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Files.Verification;

internal sealed class FileSystemMerchantVerificationPrivateFileStore :
    IMerchantVerificationPrivateFileStore
{
    private const string RootPathConfigurationKey =
        "MerchantVerification:PrivateFileStore:RootPath";

    private readonly string
        _rootPath;

    public FileSystemMerchantVerificationPrivateFileStore(
        IConfiguration configuration)
    {
        var configured =
            configuration[
                RootPathConfigurationKey];

        _rootPath =
            Path.GetFullPath(
                string.IsNullOrWhiteSpace(
                    configured)
                    ? Path.Combine(
                        AppContext.BaseDirectory,
                        "App_Data",
                        "merchant-verification")
                    : configured);

        Directory.CreateDirectory(
            _rootPath);
    }

    public async Task<MerchantVerificationPrivateFileResult>
        StoreAsync(
            TenantId tenantId,
            MerchantVerificationProfileId profileId,
            MerchantVerificationDocumentId documentId,
            MerchantVerificationDocumentSide side,
            MerchantVerificationPrivateFileUpload file,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            file);

        if (file.FileSizeBytes <= 0 ||
            file.FileSizeBytes >
                MerchantVerificationDocumentFile.MaxFileSizeBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(file),
                "Merchant verification file size is invalid.");
        }

        var storageKey =
            string.Join(
                '/',
                tenantId.Value.ToString(
                    "N"),
                profileId.Value.ToString(
                    "N"),
                documentId.Value.ToString(
                    "N"),
                ((int)side).ToString(),
                $"{Guid.NewGuid():N}.blob");

        var fullPath =
            ResolvePath(
                tenantId,
                storageKey);

        var directory =
            Path.GetDirectoryName(
                fullPath)!;

        Directory.CreateDirectory(
            directory);

        long totalBytes =
            0;

        using var hash =
            IncrementalHash.CreateHash(
                HashAlgorithmName.SHA256);

        try
        {
            await using var destination =
                new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize:
                        81920,
                    useAsync:
                        true);

            var buffer =
                new byte[81920];

            while (true)
            {
                var read =
                    await file.Content.ReadAsync(
                        buffer.AsMemory(
                            0,
                            buffer.Length),
                        cancellationToken);

                if (read == 0)
                {
                    break;
                }

                totalBytes +=
                    read;

                if (totalBytes >
                    MerchantVerificationDocumentFile.MaxFileSizeBytes)
                {
                    throw new InvalidOperationException(
                        "Merchant verification file exceeded the maximum permitted size.");
                }

                hash.AppendData(
                    buffer,
                    0,
                    read);

                await destination.WriteAsync(
                    buffer.AsMemory(
                        0,
                        read),
                    cancellationToken);
            }

            await destination.FlushAsync(
                cancellationToken);

            if (totalBytes !=
                file.FileSizeBytes)
            {
                throw new InvalidOperationException(
                    "Uploaded verification file size did not match the declared size.");
            }

            return new MerchantVerificationPrivateFileResult(
                storageKey,
                Convert.ToHexString(
                        hash.GetHashAndReset())
                    .ToLowerInvariant(),
                totalBytes,
                file.ContentType
                    .Trim()
                    .ToLowerInvariant());
        }
        catch
        {
            try
            {
                if (File.Exists(
                        fullPath))
                {
                    File.Delete(
                        fullPath);
                }
            }
            catch
            {
            }

            throw;
        }
    }

    public Task<Stream>
        OpenReadAsync(
            TenantId tenantId,
            string storageKey,
            CancellationToken cancellationToken = default)
    {
        var fullPath =
            ResolvePath(
                tenantId,
                storageKey);

        if (!File.Exists(
                fullPath))
        {
            throw new FileNotFoundException(
                "Private merchant verification file was not found.");
        }

        Stream stream =
            new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize:
                    81920,
                useAsync:
                    true);

        return Task.FromResult(
            stream);
    }

    public Task DeleteAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath =
            ResolvePath(
                tenantId,
                storageKey);

        if (File.Exists(
                fullPath))
        {
            File.Delete(
                fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(
        TenantId tenantId,
        string storageKey)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(
                storageKey))
        {
            throw new ArgumentException(
                "Private storage key is required.",
                nameof(storageKey));
        }

        var normalizedKey =
            storageKey
                .Trim()
                .Replace(
                    '\\',
                    '/');

        var expectedTenantPrefix =
            tenantId.Value.ToString(
                "N") +
            "/";

        if (!normalizedKey.StartsWith(
                expectedTenantPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Private verification storage key does not belong to the requested tenant.");
        }

        var relativePath =
            normalizedKey.Replace(
                '/',
                Path.DirectorySeparatorChar);

        var fullPath =
            Path.GetFullPath(
                Path.Combine(
                    _rootPath,
                    relativePath));

        var rootWithSeparator =
            _rootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        var comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        if (!fullPath.StartsWith(
                rootWithSeparator,
                comparison))
        {
            throw new UnauthorizedAccessException(
                "Private verification storage path is invalid.");
        }

        return fullPath;
    }
}