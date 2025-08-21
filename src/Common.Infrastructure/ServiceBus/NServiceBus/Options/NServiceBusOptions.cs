namespace Common.Infrastructure.ServiceBus.NServiceBus.Options
{
    public class NServiceBusOptions
    {
        public int ImmediateRetries { get; set; } = 0;

        public int DelayedRetries { get; set; } = 0;

        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(1);

        public string? AzureConnectionString { get; set; }

        public string? RabbitMQConnectionString { get; set; }
    }
}
