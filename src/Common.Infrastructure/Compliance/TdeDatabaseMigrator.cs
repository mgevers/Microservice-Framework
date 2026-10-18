using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Infrastructure.Compliance;

/// <summary>
/// Migrates the database, then checks that Postgres stores every table encrypted with pg_tde.
/// The service stops if it does not, so it never runs against plain tables by mistake.
/// </summary>
/// <param name="afterMigration">Runs after the checks. A service uses it to give the API role its rights.</param>
public class TdeDatabaseMigrator<TContext>(
    IServiceScopeFactory scopeFactory,
    Func<IServiceProvider, TContext, CancellationToken, Task>? afterMigration = null) : IHostedService
    where TContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await PostgresTde.EnsureTablesAreEncrypted(dbContext, cancellationToken);

        // A context with its own schema keeps its tables there, so that schema is checked too.
        var schema = dbContext.Model.GetDefaultSchema();
        if (schema is not null && schema != "public")
        {
            await PostgresTde.EnsureTablesAreEncrypted(dbContext, schema, cancellationToken);
        }

        if (afterMigration is not null)
        {
            await afterMigration(scope.ServiceProvider, dbContext, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class TdeDatabaseMigrationExtensions
{
    /// <summary>Migrates the database when the host starts. Only the message processor calls this.</summary>
    public static IServiceCollection AddTdeDatabaseMigration<TContext>(
        this IServiceCollection services,
        Func<IServiceProvider, TContext, CancellationToken, Task>? afterMigration = null)
        where TContext : DbContext =>
        services.AddHostedService(serviceProvider =>
            new TdeDatabaseMigrator<TContext>(serviceProvider.GetRequiredService<IServiceScopeFactory>(), afterMigration));
}
