using Microsoft.Extensions.Configuration;
using OFOQ.Market.Application.Common.Files;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Files.Verification;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class MerchantVerificationPrivateFileStoreTests
{
    [Fact]
    public async Task StoreOpenDelete_UsesPrivateTenantScopedStorage()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                "ofoq-kyc-tests",
                Guid.NewGuid().ToString("N"));

        try
        {
            var configuration =
                new ConfigurationManager();

            configuration[
                "MerchantVerification:PrivateFileStore:RootPath"] =
                    root;

            var store =
                new FileSystemMerchantVerificationPrivateFileStore(
                    configuration);

            var tenantId =
                TenantId.New();

            var otherTenantId =
                TenantId.New();

            var profileId =
                MerchantVerificationProfileId.New();

            var documentId =
                MerchantVerificationDocumentId.New();

            var bytes =
                "super-private-verification-document"u8
                    .ToArray();

            await using var uploadStream =
                new MemoryStream(
                    bytes);

            var stored =
                await store.StoreAsync(
                    tenantId,
                    profileId,
                    documentId,
                    MerchantVerificationDocumentSide.Front,
                    new MerchantVerificationPrivateFileUpload(
                        "identity.jpg",
                        "image/jpeg",
                        bytes.LongLength,
                        uploadStream));

            Assert.NotEmpty(
                stored.StorageKey);

            Assert.Equal(
                bytes.LongLength,
                stored.FileSizeBytes);

            Assert.Equal(
                64,
                stored.Sha256.Length);

            byte[] openedBytes;

            await using (
                var opened =
                    await store.OpenReadAsync(
                        tenantId,
                        stored.StorageKey))
            {
                using var memory =
                    new MemoryStream();

                await opened.CopyToAsync(
                    memory);

                openedBytes =
                    memory.ToArray();
            }

            Assert.Equal(
                bytes,
                openedBytes);

            await Assert.ThrowsAsync<
                UnauthorizedAccessException>(
                async () =>
                {
                    await using var _ =
                        await store.OpenReadAsync(
                            otherTenantId,
                            stored.StorageKey);
                });

            await store.DeleteAsync(
                tenantId,
                stored.StorageKey);

            await Assert.ThrowsAsync<
                FileNotFoundException>(
                async () =>
                {
                    await using var _ =
                        await store.OpenReadAsync(
                            tenantId,
                            stored.StorageKey);
                });
        }
        finally
        {
            if (Directory.Exists(
                    root))
            {
                Directory.Delete(
                    root,
                    recursive:
                        true);
            }
        }
    }
}