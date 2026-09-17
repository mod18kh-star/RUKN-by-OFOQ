using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.GoogleSignIn;

public sealed class GoogleSignInHandler
{
    private static readonly TimeSpan MfaChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly IGoogleIdentityTokenValidator _tokenValidator;
    private readonly IUserExternalLoginRepository _externalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserMfaRepository _userMfaRepository;
    private readonly IMfaLoginChallengeRepository _challengeRepository;
    private readonly IMfaLoginChallengeTokenService _challengeTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly TimeProvider _timeProvider;

    public GoogleSignInHandler(
        IGoogleIdentityTokenValidator tokenValidator,
        IUserExternalLoginRepository externalLoginRepository,
        IUserRepository userRepository,
        IUserMfaRepository userMfaRepository,
        IMfaLoginChallengeRepository challengeRepository,
        IMfaLoginChallengeTokenService challengeTokenService,
        IUnitOfWork unitOfWork,
        ITransactionExecutor transactionExecutor,
        TimeProvider timeProvider)
    {
        _tokenValidator = tokenValidator;
        _externalLoginRepository = externalLoginRepository;
        _userRepository = userRepository;
        _userMfaRepository = userMfaRepository;
        _challengeRepository = challengeRepository;
        _challengeTokenService = challengeTokenService;
        _unitOfWork = unitOfWork;
        _transactionExecutor = transactionExecutor;
        _timeProvider = timeProvider;
    }

    public async Task<GoogleSignInResult> HandleAsync(
        string idToken,
        string nonce,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 12000 ||
            string.IsNullOrWhiteSpace(nonce) || nonce.Length > 512)
        {
            throw new GoogleSignInRejectedException();
        }

        GoogleIdentityPrincipal principal;

        try
        {
            principal = await _tokenValidator.ValidateAsync(idToken.Trim(), nonce.Trim(), cancellationToken);
        }
        catch (InvalidGoogleIdentityTokenException)
        {
            throw new GoogleSignInRejectedException();
        }

        if (!principal.EmailVerified || string.IsNullOrWhiteSpace(principal.Subject))
            throw new GoogleSignInRejectedException();

        EmailAddress email;
        try
        {
            email = EmailAddress.Create(principal.Email);
        }
        catch (ArgumentException)
        {
            throw new GoogleSignInRejectedException();
        }

        var now = _timeProvider.GetUtcNow();

        var user = await _transactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                var linked = await _externalLoginRepository.GetByProviderSubjectAsync(
                    UserExternalLogin.GoogleProvider,
                    principal.Subject,
                    transactionCancellationToken);

                if (linked is not null)
                {
                    var linkedUser = await _userRepository.GetByIdAsync(linked.UserId, transactionCancellationToken);
                    EnsureUserAvailable(linkedUser);
                    return linkedUser!;
                }

                var localUser = await _userRepository.GetByEmailAsync(email, transactionCancellationToken);

                if (localUser is null)
                {
                    // Google sign-in intentionally links only to an existing local account.
                    // Privileged tenant Back Office access remains password + MFA and we do
                    // not create passwordless merchant accounts that could never satisfy it.
                    throw new GoogleSignInRejectedException();
                }

                EnsureUserAvailable(localUser);

                var existingProviderLink = await _externalLoginRepository.GetByUserAndProviderAsync(
                    localUser.Id,
                    UserExternalLogin.GoogleProvider,
                    transactionCancellationToken);

                if (existingProviderLink is not null &&
                    !string.Equals(existingProviderLink.Subject, principal.Subject, StringComparison.Ordinal))
                {
                    throw new GoogleAccountAlreadyLinkedException();
                }

                localUser.MarkEmailVerified(now, localUser.Id.Value);

                await _externalLoginRepository.AddAsync(
                    UserExternalLogin.Create(
                        localUser.Id,
                        UserExternalLogin.GoogleProvider,
                        principal.Subject,
                        email.Value,
                        now),
                    transactionCancellationToken);

                await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
                return localUser;
            },
            cancellationToken);

        var mfa = await _userMfaRepository.GetByUserIdAsync(user.Id, cancellationToken);

        if (mfa?.Status != UserMfaStatus.Enabled)
        {
            return new GoogleSignInResult(user.Id, user.Email.Value, false, null, null);
        }

        var activeChallenges = await _challengeRepository.GetActiveByUserIdAsync(user.Id, now, cancellationToken);
        foreach (var activeChallenge in activeChallenges)
            activeChallenge.Revoke(now, user.Id.Value);

        var generated = _challengeTokenService.Create();
        var expiresAtUtc = now.Add(MfaChallengeLifetime);

        await _challengeRepository.AddAsync(
            MfaLoginChallenge.Create(
                user.Id,
                generated.TokenHash,
                expiresAtUtc,
                now,
                user.Id.Value,
                UserSessionAuthenticationMethod.Google),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new GoogleSignInResult(
            user.Id,
            user.Email.Value,
            true,
            generated.Token,
            expiresAtUtc);
    }

    private static void EnsureUserAvailable(User? user)
    {
        if (user is null || user.IsDeleted || user.Status != UserStatus.Active)
            throw new GoogleSignInRejectedException();
    }
}
