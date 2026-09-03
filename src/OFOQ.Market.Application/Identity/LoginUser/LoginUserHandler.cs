using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.LoginUser;

public sealed class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenService _accessTokenService;
    private readonly TimeProvider _timeProvider;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenService = accessTokenService;
        _timeProvider = timeProvider;
    }

    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrEmpty(command.Password) ||
            command.Password.Length > 128)
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
            await _userRepository.GetByEmailAsync(
                email,
                cancellationToken);

        if (user is null ||
            user.Status != UserStatus.Active)
        {
            throw new InvalidCredentialsException();
        }

        var passwordIsValid =
            _passwordHasher.Verify(
                user.PasswordHash,
                command.Password);

        if (!passwordIsValid)
        {
            throw new InvalidCredentialsException();
        }

        var now =
            _timeProvider.GetUtcNow();

        var token =
            _accessTokenService.Create(
                user.Id,
                user.Email.Value,
                now);

        return new LoginUserResult(
            user.Id,
            user.Email.Value,
            token.Token,
            token.ExpiresAtUtc);
    }
}