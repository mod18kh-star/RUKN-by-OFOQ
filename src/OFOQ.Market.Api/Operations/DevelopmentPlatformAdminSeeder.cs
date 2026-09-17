using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Operations;

public static class DevelopmentPlatformAdminSeeder
{
    private const string Command =
        "--provision-platform-admin";

    private const string EmailEnvironmentVariable =
        "RUKN_PLATFORM_ADMIN_EMAIL";

    private const string PasswordEnvironmentVariable =
        "RUKN_PLATFORM_ADMIN_PASSWORD";

    public static bool IsRequested(
        string[] args)
    {
        return args.Any(
            argument =>
                string.Equals(
                    argument,
                    Command,
                    StringComparison.OrdinalIgnoreCase));
    }

    public static async Task RunAsync(
        WebApplication app,
        CancellationToken cancellationToken = default)
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Platform administrator provisioning is available only in Development.");
        }

        var emailValue =
            Environment.GetEnvironmentVariable(
                EmailEnvironmentVariable);

        var password =
            Environment.GetEnvironmentVariable(
                PasswordEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(emailValue))
        {
            throw new InvalidOperationException(
                $"{EmailEnvironmentVariable} is required.");
        }

        if (string.IsNullOrEmpty(password) ||
            password.Length < 8 ||
            password.Length > 128)
        {
            throw new InvalidOperationException(
                $"{PasswordEnvironmentVariable} must contain between 8 and 128 characters.");
        }

        var email =
            EmailAddress.Create(
                emailValue);

        await using var scope =
            app.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<MarketDbContext>();

        var passwordHasher =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHasher>();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            await dbContext
                .Set<User>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Email == email,
                    cancellationToken);

        if (user is not null &&
            user.IsDeleted)
        {
            throw new InvalidOperationException(
                "A deleted user already exists with this email. Restore or change that account before provisioning platform access.");
        }

        if (user is null)
        {
            user =
                User.Create(
                    email.Value,
                    passwordHasher.Hash(
                        password),
                    now);

            user.MarkEmailVerified(
                now);

            dbContext.Add(
                user);
        }
        else
        {
            user.ChangePasswordHash(
                passwordHasher.Hash(
                    password),
                now);

            user.Activate(
                now);

            user.MarkEmailVerified(
                now);
        }

        var existingAdministrator =
            await dbContext
                .Set<PlatformUserRoleAssignment>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    assignment =>
                        assignment.UserId == user.Id &&
                        assignment.Role == PlatformRole.PlatformAdministrator &&
                        !assignment.IsDeleted,
                    cancellationToken);

        if (existingAdministrator is null)
        {
            dbContext.Add(
                PlatformUserRoleAssignment.Create(
                    user.Id,
                    PlatformRole.PlatformAdministrator,
                    now,
                    user.Id.Value));
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        Console.WriteLine(
            $"Platform administrator ready: {user.Email.Value}");

        Console.WriteLine(
            existingAdministrator is null
                ? "PlatformAdministrator role granted."
                : "PlatformAdministrator role already existed.");

        Console.WriteLine(
            "Provisioning finished. No web server was started.");
    }
}
