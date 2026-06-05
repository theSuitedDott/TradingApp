using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.Entities;
using TradingApp.Services;

namespace TradingApp.Data;

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string Email { get; init; } = "admin@tradingapp.local";

    public string Password { get; init; } = string.Empty;
}

public static class DatabaseInitializer
{
    public static async Task MigrateAndSeedAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied.");

        if (app.Environment.IsDevelopment())
        {
            await SeedAdminUserAsync(scope.ServiceProvider, cancellationToken);
        }
    }

    private static async Task SeedAdminUserAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var seedOptions = services.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        if (string.IsNullOrWhiteSpace(seedOptions.Password))
        {
            return;
        }

        var email = seedOptions.Email.Trim().ToLowerInvariant();

        var adminExists = await dbContext.Users
            .AnyAsync(u => u.Role == UserRole.Admin, cancellationToken);

        if (adminExists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.Hash(seedOptions.Password),
            FirstName = "System",
            LastName = "Admin",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
