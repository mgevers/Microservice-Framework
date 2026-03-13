using Common.Testing.Integration.ConnectionStrings;
using MassTransit;
using Testcontainers.RabbitMq;
using Xunit;

namespace Common.Testing.Integration.Containers;

public class RabbitMqContainer : IAsyncLifetime
{
    public Testcontainers.RabbitMq.RabbitMqContainer Container { get; } = new RabbitMqBuilder("rabbitmq:3.11")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Container.StopAsync();
    }

    public RabbitMqTransportOptions GetOptions()
    {
        var connection = new RabbitMqConnectionString(Container.GetConnectionString());

        return new RabbitMqTransportOptions()
        {
            Host = connection.Host,
            User = connection.User,
            Pass = connection.Password,
            Port = connection.Port,
        };
    }
}
