using Common.Testing.Integration.ConnectionStrings;
using MassTransit;
using Testcontainers.RabbitMq;
using Xunit;

namespace Common.Testing.Integration.Containers;

public class RabbitMqContainer : IAsyncLifetime
{
    public Testcontainers.RabbitMq.RabbitMqContainer Container { get; } = new RabbitMqBuilder()
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public Task InitializeAsync()
    {
        return Container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Container.StopAsync();
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
