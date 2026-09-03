using Microsoft.AspNetCore.DataProtection;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Infrastructure.Security;

internal sealed class DataProtectionMfaSecretProtector :
    IMfaSecretProtector
{
    private const string Purpose =
        "OFOQ.Market.Identity.Mfa.TotpSecret.v1";

    private readonly IDataProtector _protector;

    public DataProtectionMfaSecretProtector(
        IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(
            dataProtectionProvider);

        _protector =
            dataProtectionProvider.CreateProtector(
                Purpose);
    }

    public string Protect(
        string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            secret);

        return _protector.Protect(
            secret);
    }

    public string Unprotect(
        string protectedSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            protectedSecret);

        return _protector.Unprotect(
            protectedSecret);
    }
}