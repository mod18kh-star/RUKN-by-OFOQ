using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.TrustedDevices;

public sealed record TrustedDeviceIssueResult(
    UserTrustedDeviceId DeviceId,
    string RawToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record TrustedDeviceResult(
    Guid DeviceId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastUsedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? CreatedIpAddress,
    string? LastIpAddress,
    bool IsRevoked);

public sealed class TrustedDeviceService
{
    private static readonly TimeSpan Lifetime =
        TimeSpan.FromDays(30);

    private readonly IUserTrustedDeviceRepository _repository;
    private readonly ITrustedDeviceTokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public TrustedDeviceService(
        IUserTrustedDeviceRepository repository,
        ITrustedDeviceTokenService tokenService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<TrustedDeviceIssueResult> IssueAsync(
        UserId userId,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (userId.IsEmpty)
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));

        var now =
            _timeProvider.GetUtcNow();

        var id =
            UserTrustedDeviceId.New();

        var material =
            _tokenService.Create(
                id);

        var expiresAtUtc =
            now.Add(
                Lifetime);

        var device =
            UserTrustedDevice.Create(
                id,
                userId,
                material.TokenHash,
                _tokenService.HashUserAgent(
                    userAgent),
                expiresAtUtc,
                now,
                ipAddress);

        await _repository.AddAsync(
            device,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new TrustedDeviceIssueResult(
            device.Id,
            material.Token,
            device.ExpiresAtUtc);
    }

    public async Task<bool> ValidateAsync(
        UserId userId,
        string? rawToken,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (userId.IsEmpty)
            return false;

        if (!_tokenService.TryHash(
                rawToken,
                out var deviceId,
                out var presentedHash))
        {
            return false;
        }

        var device =
            await _repository.GetByIdAsync(
                deviceId,
                cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        if (device is null ||
            device.UserId != userId ||
            !device.IsUsable(now) ||
            !_tokenService.FixedTimeEquals(
                device.TokenHash,
                presentedHash))
        {
            return false;
        }

        var currentAgentHash =
            _tokenService.HashUserAgent(
                userAgent);

        if (!string.Equals(
                device.UserAgentHash,
                currentAgentHash,
                StringComparison.Ordinal))
        {
            return false;
        }

        device.Touch(
            now,
            ipAddress);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<TrustedDeviceResult>>
        GetAllAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        var devices =
            await _repository.GetByUserIdAsync(
                userId,
                cancellationToken);

        return devices
            .Select(
                device =>
                    new TrustedDeviceResult(
                        device.Id.Value,
                        device.CreatedAtUtc,
                        device.LastUsedAtUtc,
                        device.ExpiresAtUtc,
                        device.CreatedIpAddress,
                        device.LastIpAddress,
                        device.IsRevoked))
            .ToArray();
    }

    public async Task<bool> RevokeAsync(
        UserId userId,
        UserTrustedDeviceId deviceId,
        CancellationToken cancellationToken = default)
    {
        var device =
            await _repository.GetByIdAsync(
                deviceId,
                cancellationToken);

        if (device is null ||
            device.UserId != userId)
        {
            return false;
        }

        device.Revoke(
            "user_revoked",
            _timeProvider.GetUtcNow());

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task RevokeAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var now =
            _timeProvider.GetUtcNow();

        var devices =
            await _repository.GetByUserIdAsync(
                userId,
                cancellationToken);

        foreach (var device in devices)
        {
            device.Revoke(
                "user_revoked_all",
                now);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }
}
