using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class FakeMfaSecretProtector :
    IMfaSecretProtector
{
    public string Protect(
        string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            secret);

        return $"TEST-PROTECTED::{secret}";
    }

    public string Unprotect(
        string protectedSecret)
    {
        const string prefix =
            "TEST-PROTECTED::";

        if (string.IsNullOrWhiteSpace(
                protectedSecret) ||
            !protectedSecret.StartsWith(
                prefix,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Invalid test protected MFA secret.");
        }

        return protectedSecret[
            prefix.Length..];
    }
}