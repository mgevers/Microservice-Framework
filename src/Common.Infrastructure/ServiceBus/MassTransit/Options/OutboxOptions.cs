namespace Common.Infrastructure.ServiceBus.MassTransit.Options;

public enum OutboxDatabaseProvider
{
    SqlServer,
    PostgreSql
}

public class OutboxOptions
{
    public int DuplicateDetectionWindowMinutes { get; set; }

    public OutboxDatabaseProvider Provider { get; set; } = OutboxDatabaseProvider.SqlServer;

    /// <summary>
    /// Whether MassTransit remembers the schema of the context's tables when it builds its SQL (Postgres only).
    /// A host with several contexts in different schemas turns this off, because the remembered schema is the first context's.
    /// </summary>
    public bool EnableSchemaCaching { get; set; } = true;

    /// <summary>
    /// Whether a message that is sent or published outside a consumer also goes through the outbox, so that it is
    /// saved with the caller's changes. A host whose API sends plainly turns this off.
    /// </summary>
    public bool UseBusOutbox { get; set; } = true;
}
