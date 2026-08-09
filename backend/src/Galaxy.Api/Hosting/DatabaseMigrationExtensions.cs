using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Hosting;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(
        this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(
                "Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
