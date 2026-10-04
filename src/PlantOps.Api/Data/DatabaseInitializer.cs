using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using PlantOps.Api.Models;

namespace PlantOps.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        const int maxAttempts = 30;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PlantOpsDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
                await db.Database.EnsureCreatedAsync();
                await DbSeeder.SeedAsync(db, configuration, hasher, logger);
                logger.LogInformation("Database is ready");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsDatabaseStarting(ex))
            {
                logger.LogWarning(ex, "Database is not ready (attempt {Attempt} of {MaxAttempts})", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }

    private static bool IsDatabaseStarting(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException)
            {
                return true;
            }
        }

        return false;
    }
}
