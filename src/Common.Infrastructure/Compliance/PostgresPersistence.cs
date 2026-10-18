using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Common.Infrastructure.Compliance;

/// <summary>
/// The database settings of a service. Each service derives its own class, because the class name is the name of the
/// settings section, for example <c>AccountsDbOptions</c>.
/// </summary>
public class PostgresDbOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The database role of the API. After a migration, the message processor gives this role the least it needs.
    /// The role is not created here. The Postgres setup script creates it.
    /// </summary>
    public string? ApiRole { get; set; }
}

public static class PostgresPersistenceExtensions
{
    /// <summary>
    /// Registers the context with Postgres, the audit trail that writes into it, and the readiness check.
    /// The migration history table is named after the context, so that services can share a server.
    /// </summary>
    /// <param name="key">
    /// For a host with several contexts. The health check is named <c>postgres-{key}</c>, and the audit trail is registered
    /// keyed by it, with no unkeyed trail, so that a consumer that asks for one fails instead of writing into another context.
    /// </param>
    /// <param name="schema">For a host with several contexts. The schema of the migration history table.</param>
    public static IHostApplicationBuilder AddPostgresPersistence<TContext, TOptions>(
        this IHostApplicationBuilder builder,
        Action<TOptions> config,
        string? key = null,
        string? schema = null)
        where TContext : DbContext
        where TOptions : PostgresDbOptions
    {
        builder.Services
            .AddOptions<TOptions>()
            .Configure(config);

        builder.Services
            .AddDbContext<TContext>((serviceProvider, options) =>
            {
                var dbOptions = serviceProvider.GetRequiredService<IOptions<TOptions>>();

                options.UseNpgsql(dbOptions.Value.ConnectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name);
                    npgsql.MigrationsHistoryTable($"__{typeof(TContext).Name}", schema);
                    npgsql.EnableRetryOnFailure(5);
                });
            });

        if (key is null)
        {
            builder.Services.AddScoped<IAuditTrail>(serviceProvider => new EfAuditTrail(serviceProvider.GetRequiredService<TContext>()));
        }
        else
        {
            builder.Services.AddKeyedScoped<IAuditTrail>(key, (serviceProvider, _) => new EfAuditTrail(serviceProvider.GetRequiredService<TContext>()));
        }

        builder.Services
            .AddHealthChecks()
            .AddDbContextCheck<TContext>(key is null ? "postgres" : $"postgres-{key}", tags: ["ready"]);

        return builder;
    }

    /// <summary>
    /// Migrates the database when the host starts, checks that every table is encrypted, and gives the API role
    /// <c>SELECT</c> on <paramref name="readTables"/> and <c>INSERT</c> on <paramref name="insertTables"/>, and nothing else.
    /// The tables are those of the default schema of the context, or of <c>public</c> if it has none.
    /// Only the message processor calls this.
    /// </summary>
    public static IHostApplicationBuilder AddPostgresMigration<TContext, TOptions>(
        this IHostApplicationBuilder builder,
        IReadOnlyCollection<string> readTables,
        IReadOnlyCollection<string> insertTables)
        where TContext : DbContext
        where TOptions : PostgresDbOptions
    {
        builder.Services.AddTdeDatabaseMigration<TContext>(async (services, dbContext, cancellationToken) =>
        {
            var apiRole = services.GetRequiredService<IOptions<TOptions>>().Value.ApiRole;
            if (!string.IsNullOrWhiteSpace(apiRole))
            {
                var schema = dbContext.Model.GetDefaultSchema() ?? "public";
                await PostgresTde.GrantApiRole(dbContext, schema, apiRole, readTables, insertTables, cancellationToken);
            }
        });

        return builder;
    }

    /// <summary>The inbox and outbox tables of MassTransit, which make a write and its events one transaction.</summary>
    public static ModelBuilder ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        return modelBuilder;
    }
}
