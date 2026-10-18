using Common.Infrastructure.Compliance;
using Common.Infrastructure.ServiceBus.MassTransit;
using Common.Infrastructure.ServiceBus.MassTransit.Options;
using Common.Testing.Integration.Containers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Common.Infrastructure.Functional.Tests.ServiceBus;

public class OutboxForSeveralContextsTests : IClassFixture<PostgresDbContainer>
{
    private readonly PostgresDbContainer postgres;

    public OutboxForSeveralContextsTests(PostgresDbContainer postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task EachContextConsumesAndSavesInItsOwnSchema()
    {
        await using var provider = await StartHost(enableSchemaCaching: false);

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var bus = provider.GetRequiredService<IBus>();
        await bus.Publish(new AddNoteInA(firstId));
        await bus.Publish(new AddNoteInB(secondId));

        Assert.True(await NoteIsSaved("a", firstId));
        Assert.True(await NoteIsSaved("b", secondId));
        Assert.False(await NoteIsSaved("a", secondId));
        Assert.False(await NoteIsSaved("b", firstId));
    }

    [Fact]
    public async Task WithSchemaCachingTheSecondContextDoesNotComplete()
    {
        await using var provider = await StartHost(enableSchemaCaching: true);

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var bus = provider.GetRequiredService<IBus>();
        await bus.Publish(new AddNoteInA(firstId));
        Assert.True(await NoteIsSaved("a", firstId));

        await bus.Publish(new AddNoteInB(secondId));
        Assert.False(await NoteIsSaved("b", secondId, TimeSpan.FromSeconds(10)));
    }

    private async Task<ServiceProvider> StartHost(bool enableSchemaCaching)
    {
        var connectionString = postgres.Container.GetConnectionString();

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await new NpgsqlCommand("drop schema if exists a cascade; drop schema if exists b cascade; create schema a; create schema b;", connection)
                .ExecuteNonQueryAsync();
        }

        void Outbox(OutboxOptions options)
        {
            options.Provider = OutboxDatabaseProvider.PostgreSql;
            options.DuplicateDetectionWindowMinutes = 30;
            options.EnableSchemaCaching = enableSchemaCaching;
            options.UseBusOutbox = false;
        }

        var services = new ServiceCollection();
        services.AddDbContext<ContextA>(options => options.UseNpgsql(connectionString));
        services.AddDbContext<ContextB>(options => options.UseNpgsql(connectionString));
        services.AddMassTransitTestHarness(config =>
        {
            config.AddOutbox<ContextA>(Outbox);
            config.AddOutbox<ContextB>(Outbox);
            config.AddConsumer<ConsumerA, OutboxConsumerDefinition<ConsumerA, ContextA>>();
            config.AddConsumer<ConsumerB, OutboxConsumerDefinition<ConsumerB, ContextB>>();
        });

        var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ContextA>().GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
            await scope.ServiceProvider.GetRequiredService<ContextB>().GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
        }

        await provider.GetRequiredService<ITestHarness>().Start();

        return provider;
    }

    private async Task<bool> NoteIsSaved(string schema, Guid id, TimeSpan? wait = null)
    {
        var until = DateTime.UtcNow + (wait ?? TimeSpan.FromSeconds(15));

        await using var connection = new NpgsqlConnection(postgres.Container.GetConnectionString());
        await connection.OpenAsync();

        do
        {
            var command = new NpgsqlCommand($"select count(*) from {schema}.\"Notes\" where \"Id\" = @id", connection);
            command.Parameters.AddWithValue("id", id);

            if ((long)(await command.ExecuteScalarAsync())! > 0)
            {
                return true;
            }

            await Task.Delay(200);
        }
        while (DateTime.UtcNow < until);

        return false;
    }

    public record AddNoteInA(Guid Id);

    public record AddNoteInB(Guid Id);

    public class Note
    {
        public Guid Id { get; set; }
    }

    public class ContextA(DbContextOptions<ContextA> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("a");
            modelBuilder.Entity<Note>().ToTable("Notes");
            modelBuilder.ConfigureOutbox();
        }
    }

    public class ContextB(DbContextOptions<ContextB> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("b");
            modelBuilder.Entity<Note>().ToTable("Notes");
            modelBuilder.ConfigureOutbox();
        }
    }

    public class ConsumerA(ContextA dbContext) : IConsumer<AddNoteInA>
    {
        public async Task Consume(ConsumeContext<AddNoteInA> context)
        {
            dbContext.Set<Note>().Add(new Note { Id = context.Message.Id });
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }

    public class ConsumerB(ContextB dbContext) : IConsumer<AddNoteInB>
    {
        public async Task Consume(ConsumeContext<AddNoteInB> context)
        {
            dbContext.Set<Note>().Add(new Note { Id = context.Message.Id });
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }

    // Puts the endpoint of a consumer on the outbox of its own context.
    public class OutboxConsumerDefinition<TConsumer, TContext> : ConsumerDefinition<TConsumer>
        where TConsumer : class, IConsumer
        where TContext : DbContext
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<TConsumer> consumerConfigurator,
            IRegistrationContext context) =>
            endpointConfigurator.UseEntityFrameworkOutbox<TContext>(context);
    }
}
