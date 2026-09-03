using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Identity.RegisterUser;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Tests.Identity.RegisterUser;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesUser()
    {
        var repository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                repository,
                passwordHasher,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new RegisterUserCommand(
                    "user@example.com",
                    "StrongPassword123"));

        Assert.NotEqual(
            Guid.Empty,
            result.UserId.Value);

        Assert.Equal(
            "user@example.com",
            result.Email);

        Assert.Equal(
            UserStatus.Active,
            result.Status);

        Assert.NotNull(
            repository.AddedUser);

        Assert.Equal(
            "HASHED::StrongPassword123",
            repository.AddedUser.PasswordHash);

        Assert.NotEqual(
            "StrongPassword123",
            repository.AddedUser.PasswordHash);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_NormalizesEmail()
    {
        var repository =
            new FakeUserRepository();

        var handler =
            CreateHandler(
                repository,
                new FakePasswordHasher(),
                new FakeUnitOfWork());

        var result =
            await handler.HandleAsync(
                new RegisterUserCommand(
                    "  USER@Example.COM  ",
                    "StrongPassword123"));

        Assert.Equal(
            "user@example.com",
            result.Email);
    }

    [Fact]
    public async Task HandleAsync_RejectsDuplicateEmail()
    {
        var repository =
            new FakeUserRepository
            {
                EmailExists = true
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                repository,
                new FakePasswordHasher(),
                unitOfWork);

        await Assert.ThrowsAsync<
            UserEmailAlreadyExistsException>(
                () =>
                    handler.HandleAsync(
                        new RegisterUserCommand(
                            "user@example.com",
                            "StrongPassword123")));

        Assert.Null(
            repository.AddedUser);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_RejectsShortPassword()
    {
        var repository =
            new FakeUserRepository();

        var handler =
            CreateHandler(
                repository,
                new FakePasswordHasher(),
                new FakeUnitOfWork());

        await Assert.ThrowsAsync<
            InvalidPasswordException>(
                () =>
                    handler.HandleAsync(
                        new RegisterUserCommand(
                            "user@example.com",
                            "short")));
    }

    private static RegisterUserHandler CreateHandler(
        FakeUserRepository repository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        return new RegisterUserHandler(
            repository,
            passwordHasher,
            unitOfWork,
            TimeProvider.System);
    }

    private sealed class FakeUserRepository :
        IUserRepository
    {
        public bool EmailExists { get; set; }

        public User? AddedUser { get; private set; }

        public Task<User?> GetByIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByEmailAsync(
            EmailAddress email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<bool> EmailExistsAsync(
            EmailAddress email,
            UserId? excludingUserId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                EmailExists);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            AddedUser = user;

            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher :
        IPasswordHasher
    {
        public string Hash(
            string password)
        {
            return $"HASHED::{password}";
        }

        public bool Verify(
            string passwordHash,
            string password)
        {
            return passwordHash ==
                $"HASHED::{password}";
        }
    }

    private sealed class FakeUnitOfWork :
        IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;

            return Task.FromResult(1);
        }
    }
}