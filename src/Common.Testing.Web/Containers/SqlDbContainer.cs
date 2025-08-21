using Testcontainers.MsSql;
using Xunit;

namespace Common.Testing.Integration.Containers;

public class SqlDbContainer : IAsyncLifetime
{
    public MsSqlContainer Container { get; } = new MsSqlBuilder().Build();

    public Task InitializeAsync()
    {
        return Container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Container.StopAsync();
    }
}
