using Microsoft.AspNetCore.Identity;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Infrastructure.Security;

public sealed class AspNetPasswordHasher :
    IPasswordHasher
{
    private const string DummyPassword =
        "OFOQ-DUMMY-PASSWORD-DO-NOT-USE";

    private readonly PasswordHasher<object> _hasher =
        new();

    private readonly object _context =
        new();

    private readonly string _dummyPasswordHash;

    public AspNetPasswordHasher()
    {
        _dummyPasswordHash =
            _hasher.HashPassword(
                _context,
                DummyPassword);
    }

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

    public void PerformDummyVerification(
        string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(
            password);

        _ = _hasher.VerifyHashedPassword(
            _context,
            _dummyPasswordHash,
            password);
    }
}