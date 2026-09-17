using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Sessions;

public sealed class AuthenticationSessionService
{
    private static readonly TimeSpan SessionLifetime =
        TimeSpan.FromDays(30);

    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISessionAccessTokenService _accessTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly TimeProvider _timeProvider;

    public AuthenticationSessionService(
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        IRefreshTokenService refreshTokenService,
        ISessionAccessTokenService accessTokenService,
        IUnitOfWork unitOfWork,
        ITransactionExecutor transactionExecutor,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _refreshTokenService = refreshTokenService;
        _accessTokenService = accessTokenService;
        _unitOfWork = unitOfWork;
        _transactionExecutor = transactionExecutor;
        _timeProvider = timeProvider;
    }

    public Task<AuthenticationSessionResult> IssueAsync(
        UserId userId,
        UserSessionAuthenticationLevel authenticationLevel,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        return IssueAsync(
            userId,
            UserSessionAuthenticationMethod.Password,
            authenticationLevel,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<AuthenticationSessionResult> IssueAsync(
        UserId userId,
        UserSessionAuthenticationMethod authenticationMethod,
        UserSessionAuthenticationLevel authenticationLevel,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var user =
            await _userRepository.GetByIdAsync(
                userId,
                cancellationToken);

        EnsureUserAvailable(user);

        var sessionId = UserSessionId.New();
        var refresh = _refreshTokenService.Create(sessionId);
        var expiresAtUtc = now.Add(SessionLifetime);

        var session = UserSession.Create(
            sessionId,
            user!.Id,
            refresh.TokenHash,
            authenticationLevel,
            expiresAtUtc,
            now,
            NormalizeIp(ipAddress),
            NormalizeUserAgent(userAgent),
            authenticationMethod);

        await _sessionRepository.AddAsync(
            session,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return CreateResult(
            user,
            session,
            refresh.Token,
            now);
    }

    public async Task<AuthenticationSessionResult> UpgradeOrIssueAsync(
        UserId userId,
        UserSessionId? currentSessionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var user =
            await _userRepository.GetByIdAsync(
                userId,
                cancellationToken);

        EnsureUserAvailable(user);

        if (currentSessionId.HasValue)
        {
            var current =
                await _sessionRepository.GetByIdAsync(
                    currentSessionId.Value,
                    cancellationToken);

            if (current is not null &&
                current.UserId == userId &&
                current.IsUsable(now))
            {
                var refresh =
                    _refreshTokenService.Create(
                        current.Id);

                current.UpgradeToMultiFactor(
                    refresh.TokenHash,
                    now,
                    NormalizeIp(ipAddress),
                    NormalizeUserAgent(userAgent));

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return CreateResult(
                    user!,
                    current,
                    refresh.Token,
                    now);
            }
        }

        return await IssueAsync(
            userId,
            UserSessionAuthenticationLevel.MultiFactor,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<AuthenticationSessionResult> RefreshAsync(
        string rawRefreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (!_refreshTokenService.TryHash(
                rawRefreshToken,
                out var sessionId,
                out var presentedHash))
        {
            throw new InvalidAuthenticationSessionException();
        }

        var now = _timeProvider.GetUtcNow();

        RefreshOutcome outcome;

        try
        {
            outcome = await _transactionExecutor.ExecuteAsync(
                async transactionCancellationToken =>
                {
                    var session =
                        await _sessionRepository.GetByIdAsync(
                            sessionId,
                            transactionCancellationToken);

                    if (session is null ||
                        !session.IsUsable(now))
                    {
                        return RefreshOutcome.Failed();
                    }

                    if (!_refreshTokenService.FixedTimeEquals(
                            session.RefreshTokenHash,
                            presentedHash))
                    {
                        session.Revoke(
                            "refresh_token_reuse_or_mismatch",
                            now);

                        await _unitOfWork.SaveChangesAsync(
                            transactionCancellationToken);

                        return RefreshOutcome.Failed();
                    }

                    var user =
                        await _userRepository.GetByIdAsync(
                            session.UserId,
                            transactionCancellationToken);

                    if (user is null ||
                        user.IsDeleted ||
                        user.Status != UserStatus.Active)
                    {
                        session.Revoke(
                            "account_unavailable",
                            now);

                        await _unitOfWork.SaveChangesAsync(
                            transactionCancellationToken);

                        return RefreshOutcome.Failed();
                    }

                    var refresh =
                        _refreshTokenService.Create(
                            session.Id);

                    session.RotateRefreshToken(
                        refresh.TokenHash,
                        now,
                        NormalizeIp(ipAddress),
                        NormalizeUserAgent(userAgent));

                    await _unitOfWork.SaveChangesAsync(
                        transactionCancellationToken);

                    return RefreshOutcome.Succeeded(
                        user,
                        session,
                        refresh.Token);
                },
                cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            throw new InvalidAuthenticationSessionException();
        }

        if (!outcome.IsSuccessful ||
            outcome.User is null ||
            outcome.Session is null ||
            outcome.RefreshToken is null)
        {
            throw new InvalidAuthenticationSessionException();
        }

        return CreateResult(
            outcome.User,
            outcome.Session,
            outcome.RefreshToken,
            now);
    }

    public async Task LogoutAsync(
        string? rawRefreshToken,
        CancellationToken cancellationToken = default)
    {
        if (!_refreshTokenService.TryHash(
                rawRefreshToken,
                out var sessionId,
                out var presentedHash))
        {
            return;
        }

        var session =
            await _sessionRepository.GetByIdAsync(
                sessionId,
                cancellationToken);

        if (session is null ||
            !_refreshTokenService.FixedTimeEquals(
                session.RefreshTokenHash,
                presentedHash))
        {
            return;
        }

        session.Revoke(
            "user_logout",
            _timeProvider.GetUtcNow());

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuthenticationSessionSummary>> ListAsync(
        UserId userId,
        UserSessionId? currentSessionId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var sessions =
            await _sessionRepository.GetActiveByUserIdAsync(
                userId,
                now,
                cancellationToken);

        return sessions
            .OrderByDescending(session => session.LastSeenAtUtc)
            .Select(
                session =>
                    new AuthenticationSessionSummary(
                        session.Id,
                        session.AuthenticationLevel,
                        session.CreatedAtUtc,
                        session.LastSeenAtUtc,
                        session.ExpiresAtUtc,
                        session.LastIpAddress,
                        session.UserAgent,
                        currentSessionId.HasValue &&
                        session.Id == currentSessionId.Value))
            .ToArray();
    }

    public async Task<bool> RevokeAsync(
        UserId userId,
        UserSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        var session =
            await _sessionRepository.GetByIdAsync(
                sessionId,
                cancellationToken);

        if (session is null ||
            session.UserId != userId)
        {
            return false;
        }

        session.Revoke(
            "user_revoked_session",
            _timeProvider.GetUtcNow());

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<int> RevokeOthersAsync(
        UserId userId,
        UserSessionId currentSessionId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var sessions =
            await _sessionRepository.GetActiveByUserIdAsync(
                userId,
                now,
                cancellationToken);

        var revoked = 0;

        foreach (var session in sessions)
        {
            if (session.Id == currentSessionId)
            {
                continue;
            }

            session.Revoke(
                "user_revoked_other_sessions",
                now);

            revoked++;
        }

        if (revoked > 0)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return revoked;
    }

    private AuthenticationSessionResult CreateResult(
        User user,
        UserSession session,
        string rawRefreshToken,
        DateTimeOffset now)
    {
        var tokenLevel =
            session.AuthenticationLevel ==
                UserSessionAuthenticationLevel.MultiFactor
                ? AccessTokenAuthenticationLevel.MultiFactor
                : AccessTokenAuthenticationLevel.PasswordOnly;

        var access =
            _accessTokenService.Create(
                user.Id,
                user.Email.Value,
                now,
                tokenLevel,
                session.AuthenticationMethod,
                session.Id);

        return new AuthenticationSessionResult(
            session.Id,
            access.Token,
            access.ExpiresAtUtc,
            rawRefreshToken,
            session.ExpiresAtUtc);
    }

    private static void EnsureUserAvailable(
        User? user)
    {
        if (user is null ||
            user.IsDeleted ||
            user.Status != UserStatus.Active)
        {
            throw new InvalidAuthenticationSessionException();
        }
    }

    private static string? NormalizeIp(
        string? value)
    {
        return NormalizeMetadata(value, 64);
    }

    private static string? NormalizeUserAgent(
        string? value)
    {
        return NormalizeMetadata(value, 512);
    }

    private static string? NormalizeMetadata(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private sealed record RefreshOutcome(
        bool IsSuccessful,
        User? User,
        UserSession? Session,
        string? RefreshToken)
    {
        public static RefreshOutcome Failed()
        {
            return new RefreshOutcome(
                false,
                null,
                null,
                null);
        }

        public static RefreshOutcome Succeeded(
            User user,
            UserSession session,
            string refreshToken)
        {
            return new RefreshOutcome(
                true,
                user,
                session,
                refreshToken);
        }
    }
}

