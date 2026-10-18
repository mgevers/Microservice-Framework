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
}
