namespace Common.Infrastructure.ServiceBus.MassTransit.Options;

public class OutboxOptions
{
    public int DuplicateDetectionWindowMinutes { get; set; }
}
