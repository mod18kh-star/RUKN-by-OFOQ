using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.RegisterUser;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email =
            EmailAddress.Create(command.Email);

        ValidatePassword(
            command.Password);

        if (await _userRepository.EmailExistsAsync(
                email,
                cancellationToken: cancellationToken))
        {
            throw new UserEmailAlreadyExistsException(
                email.Value);
        }

        var passwordHash =
            _passwordHasher.Hash(
                command.Password);

        var now =
            _timeProvider.GetUtcNow();

        var user =
            User.Create(
                email.Value,
                passwordHash,
                now);

        await _userRepository.AddAsync(
            user,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new RegisterUserResult(
            user.Id,
            user.Email.Value,
            user.Status,
            user.CreatedAtUtc);
    }

    private static void ValidatePassword(
        string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidPasswordException(
                "Password is required.");
        }

        if (password.Length < 8)
        {
            throw new InvalidPasswordException(
                "Password must contain at least 8 characters.");
        }

        if (password.Length > 128)
        {
            throw new InvalidPasswordException(
                "Password cannot exceed 128 characters.");
        }
    }
}