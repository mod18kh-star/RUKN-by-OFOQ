using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.LoginUser;

public sealed class LoginUserHandler
{
    private static readonly TimeSpan MfaChallengeLifetime =
        TimeSpan.FromMinutes(5);

    private readonly IUserRepository _userRepository;
    private readonly IUserMfaRepository _userMfaRepository;
    private readonly IMfaLoginChallengeRepository
        _mfaLoginChallengeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMfaLoginChallengeTokenService
        _challengeTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public LoginUserHandler(
        IUserRepository userRepository,
        IUserMfaRepository userMfaRepository,
        IMfaLoginChallengeRepository mfaLoginChallengeRepository,
        IPasswordHasher passwordHasher,
        IMfaLoginChallengeTokenService challengeTokenService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userRepository =
            userRepository;

        _userMfaRepository =
            userMfaRepository;

        _mfaLoginChallengeRepository =
            mfaLoginChallengeRepository;

        _passwordHasher =
            passwordHasher;

        _challengeTokenService =
            challengeTokenService;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (string.IsNullOrEmpty(
                command.Password)
            || command.Password.Length > 128)
        {
            throw new InvalidCredentialsException();
        }

        EmailAddress email;

        try
        {
            email =
                EmailAddress.Create(
                    command.Email);
        }
        catch (ArgumentException)
        {
            throw new InvalidCredentialsException();
        }

        var user =
            await _userRepository
                .GetByEmailAsync(
                    email,
                    cancellationToken);

        if (user is null)
        {
            _passwordHasher
                .PerformDummyVerification(
                    command.Password);

            throw new InvalidCredentialsException();
        }

        // ظ†طھط­ظ‚ظ‚ ظ…ظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط­طھظ‰ ظ„ظ„ط­ط³ط§ط¨ ط؛ظٹط± ط§ظ„ظ†ط´ط·
        // ظ„طھظ‚ظ„ظٹظ„ ظپط±ظˆظ‚ط§طھ ط§ظ„طھظˆظ‚ظٹطھ ط§ظ„طھظٹ ظ‚ط¯ طھظƒط´ظپ ط­ط§ظ„ط© ط§ظ„ط­ط³ط§ط¨.
        var passwordIsValid =
            _passwordHasher.Verify(
                user.PasswordHash,
                command.Password);

        if (!passwordIsValid ||
            user.Status != UserStatus.Active)
        {
            throw new InvalidCredentialsException();
        }

        var now =
            _timeProvider.GetUtcNow();

        var mfa =
            await _userMfaRepository
                .GetByUserIdAsync(
                    user.Id,
                    cancellationToken);

        if (mfa?.Status ==
            UserMfaStatus.Enabled)
        {
            return await CreateMfaChallengeAsync(
                user,
                now,
                cancellationToken);
        }return new LoginUserResult(
            user.Id,
            user.Email.Value,
            RequiresMfa: false,
            
            MfaChallengeToken:
                null,
            MfaChallengeExpiresAtUtc:
                null);
    }

    private async Task<LoginUserResult>
        CreateMfaChallengeAsync(
            User user,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        var activeChallenges =
            await _mfaLoginChallengeRepository
                .GetActiveByUserIdAsync(
                    user.Id,
                    now,
                    cancellationToken);

        foreach (var activeChallenge in
                 activeChallenges)
        {
            activeChallenge.Revoke(
                now,
                user.Id.Value);
        }

        var generatedToken =
            _challengeTokenService.Create();

        var expiresAtUtc =
            now.Add(
                MfaChallengeLifetime);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                generatedToken.TokenHash,
                expiresAtUtc,
                now,
                user.Id.Value);

        await _mfaLoginChallengeRepository
            .AddAsync(
                challenge,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return new LoginUserResult(
            user.Id,
            user.Email.Value,
            RequiresMfa: true,
            
            MfaChallengeToken:
                generatedToken.Token,
            MfaChallengeExpiresAtUtc:
                expiresAtUtc);
    }
}



