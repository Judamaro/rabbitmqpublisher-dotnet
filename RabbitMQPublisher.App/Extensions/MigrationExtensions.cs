namespace RabbitMQPublisher.App.Extensions;

/// <summary>
/// Applies Entity Framework Core migrations at startup.
/// </summary>
internal static class MigrationExtensions
{
    /// <summary>Configuration key that enables applying pending migrations at startup.</summary>
    public const string ApplyMigrationsOnStartupKey = "Database:ApplyMigrationsOnStartup";

    /// <summary>
    /// Applies the pending migrations of <see cref="AppDbContext"/> when
    /// <see cref="ApplyMigrationsOnStartupKey"/> is enabled in the configuration.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A task that completes when the database is up to date, or immediately when disabled.</returns>
    public static async Task ApplyMigrationsIfEnabledAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(ApplyMigrationsOnStartupKey))
        {
            return;
        }

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        app.Logger.LogInformation("Applying pending database migrations");
        await dbContext.Database.MigrateAsync();
    }
}
