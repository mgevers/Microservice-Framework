using Common.Infrastructure.Compliance;
using Common.Testing.Integration.Containers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Common.Infrastructure.Functional.Tests.Compliance;

public class SeveralContextsTests : IClassFixture<PostgresDbContainer>
{
    private readonly PostgresDbContainer postgres;

    public SeveralContextsTests(PostgresDbContainer postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task WithKeys_EachContextHasItsOwnHealthCheckAndItsOwnKeyedAuditTrail()
    {
        await using var provider = await BuildHost();

        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Select(r => r.Name).ToList();
        Assert.Equal(["postgres-a", "postgres-b"], registrations.Order());

        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal(2, report.Entries.Count);

        Assert.Null(provider.GetService<IAuditTrail>());
    }

    [Fact]
    public async Task WithKeys_EachKeyedAuditTrailWritesToItsOwnContextsTable()
    {
        await using var provider = await BuildHost();

        await using (var scope = provider.CreateAsyncScope())
        {
            var trail = scope.ServiceProvider.GetRequiredKeyedService<IAuditTrail>("b");
            await trail.Record([AuditEntry.For("someone", "read", "a-subject")]);
        }

        Assert.Equal(0, await CountEntries("a"));
        Assert.Equal(1, await CountEntries("b"));
    }

    [Fact]
    public async Task WithASchema_TheHistoryTableIsInThatSchema()
    {
        await using var provider = await BuildHost();
        await using var scope = provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ContextA>();
        await dbContext.Database.ExecuteSqlRawAsync(dbContext.GetService<IHistoryRepository>().GetCreateScript());

        Assert.Equal(1, await CountTables("a", "__ContextA"));
        Assert.Equal(0, await CountTables("public", "__ContextA"));
    }

    [Fact]
    public async Task WithoutKeyOrSchema_ItIsAsBefore()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddPostgresPersistence<ContextA, DbOptionsA>(options => options.ConnectionString = postgres.Container.GetConnectionString());
        await using var provider = builder.Services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        Assert.Equal(["postgres"], provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Select(r => r.Name));
        Assert.NotNull(scope.ServiceProvider.GetService<IAuditTrail>());
        Assert.Null(scope.ServiceProvider.GetKeyedService<IAuditTrail>("a"));
        Assert.Contains(" \"__ContextA\"", scope.ServiceProvider.GetRequiredService<ContextA>().GetService<IHistoryRepository>().GetCreateScript());
    }

    [Fact]
    public async Task KeyedAuditBehavior_RecordsToTheTrailTheKeyFunctionPicks()
    {
        await using var provider = await BuildHost(auditTrailKey: type => type == typeof(AuditedInB) ? "b" : "a");

        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new AuditedInB());
            await mediator.Send(new AuditedInB());
            await mediator.Send(new AuditedInA());
            await mediator.Send(new NotAudited());
        }

        Assert.Equal(1, await CountEntries("a"));
        Assert.Equal(2, await CountEntries("b"));
    }

    [Fact]
    public async Task KeyedAuditBehavior_FailsBeforeTheHandlerWhenItCannotRecord()
    {
        await using var provider = await BuildHost(auditTrailKey: _ => "no-such-trail");

        await using var scope = provider.CreateAsyncScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IMediator>().Send(new AuditedInA()));

        Assert.False(AuditedInAHandler.Ran);
    }

    private async Task<ServiceProvider> BuildHost(Func<Type, object>? auditTrailKey = null)
    {
        var connectionString = postgres.Container.GetConnectionString();

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await new NpgsqlCommand("drop schema if exists a cascade; drop schema if exists b cascade; create schema a; create schema b;", connection)
                .ExecuteNonQueryAsync();
        }

        AuditedInAHandler.Ran = false;

        var builder = Host.CreateApplicationBuilder();
        builder.AddPostgresPersistence<ContextA, DbOptionsA>(options => options.ConnectionString = connectionString, key: "a", schema: "a");
        builder.AddPostgresPersistence<ContextB, DbOptionsB>(options => options.ConnectionString = connectionString, key: "b", schema: "b");
        builder.Services.AddSingleton<ICurrentActor, Actor>();
        builder.Services.AddAuditedMediatR(auditTrailKey ?? (_ => "a"), typeof(SeveralContextsTests).Assembly);

        var provider = builder.Services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ContextA>().GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
            await scope.ServiceProvider.GetRequiredService<ContextB>().GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
        }

        return provider;
    }

    private async Task<long> CountTables(string schema, string table)
    {
        await using var connection = new NpgsqlConnection(postgres.Container.GetConnectionString());
        await connection.OpenAsync();

        var command = new NpgsqlCommand("select count(*) from information_schema.tables where table_schema = @schema and table_name = @table", connection);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> CountEntries(string schema)
    {
        await using var connection = new NpgsqlConnection(postgres.Container.GetConnectionString());
        await connection.OpenAsync();

        return (long)(await new NpgsqlCommand($"select count(*) from {schema}.\"AuditEntry\"", connection).ExecuteScalarAsync())!;
    }

    public class DbOptionsA : PostgresDbOptions;

    public class DbOptionsB : PostgresDbOptions;

    public class ContextA(DbContextOptions<ContextA> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("a");
            modelBuilder.ConfigureAuditEntries(64);
        }
    }

    public class ContextB(DbContextOptions<ContextB> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("b");
            modelBuilder.ConfigureAuditEntries(64);
        }
    }

    private class Actor : ICurrentActor
    {
        public string Id => "test-actor";
    }

    public record AuditedInA : IRequest<string>, IAuditedRequest
    {
        public string Action => "read";

        public string? Subject => "a";
    }

    public record AuditedInB : IRequest<string>, IAuditedRequest
    {
        public string Action => "read";

        public string? Subject => "b";
    }

    public record NotAudited : IRequest<string>;

    public class AuditedInAHandler : IRequestHandler<AuditedInA, string>
    {
        public static bool Ran { get; set; }

        public Task<string> Handle(AuditedInA request, CancellationToken cancellationToken)
        {
            Ran = true;
            return Task.FromResult("done");
        }
    }

    public class AuditedInBHandler : IRequestHandler<AuditedInB, string>
    {
        public Task<string> Handle(AuditedInB request, CancellationToken cancellationToken) => Task.FromResult("done");
    }

    public class NotAuditedHandler : IRequestHandler<NotAudited, string>
    {
        public Task<string> Handle(NotAudited request, CancellationToken cancellationToken) => Task.FromResult("done");
    }
}
