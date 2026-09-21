using System.Security.Cryptography;
using System.Text;

namespace OFOQ.Market.Api.Endpoints.Commerce;

// Encrypts uploaded receipts separately from provider credentials, with tenant/order/file-bound AAD.
internal static class ManualReceiptCrypto
{
    private const string MasterKeySetting = "Payments:CredentialProtection:MasterKey";
    private static byte[] DeriveKey(IConfiguration configuration)
    {
        var source = configuration[MasterKeySetting] ?? throw new InvalidOperationException("Payment encryption is not configured.");
        var master = Convert.FromBase64String(source);
        if (master.Length != 32) throw new InvalidOperationException("Invalid payment encryption key length.");
        try { return HMACSHA256.HashData(master, "rukn-manual-receipt-v1"u8.ToArray()); }
        finally { CryptographicOperations.ZeroMemory(master); }
    }
    private static byte[] Aad(Guid tenantId, Guid orderId, Guid receiptId) =>
        Encoding.UTF8.GetBytes($"rukn-receipt:v1:{tenantId:N}:{orderId:N}:{receiptId:N}");

    public static (byte[] Ciphertext, byte[] Nonce, byte[] Tag) Protect(
        IConfiguration configuration, Guid tenantId, Guid orderId, Guid receiptId, byte[] content)
    {
        var key = DeriveKey(configuration);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[content.Length];
        var tag = new byte[16];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, content, ciphertext, tag, Aad(tenantId, orderId, receiptId));
            return (ciphertext, nonce, tag);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
    public static byte[] Unprotect(IConfiguration configuration, Guid tenantId, Guid orderId,
        Guid receiptId, byte[] ciphertext, byte[] nonce, byte[] tag)
    {
        var key = DeriveKey(configuration);
        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, Aad(tenantId, orderId, receiptId));
            return plaintext;
        }
        catch { CryptographicOperations.ZeroMemory(plaintext); throw; }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
