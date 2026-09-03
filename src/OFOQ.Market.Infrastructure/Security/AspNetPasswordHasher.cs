using Microsoft.AspNetCore.Identity;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Infrastructure.Security;

public sealed class AspNetPasswordHasher :
    IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher =
        new();

    private readonly object _context =
        new();

    public string Hash(
        string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(
            password);

        return _hasher.HashPassword(
            _context,
            password);
    }

    public bool Verify(
        string passwordHash,
        string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(
            passwordHash);

        ArgumentException.ThrowIfNullOrEmpty(
            password);

        var result =
            _hasher.VerifyHashedPassword(
                _context,
                passwordHash,
                password);

        return result !=
            PasswordVerificationResult.Failed;
    }
}