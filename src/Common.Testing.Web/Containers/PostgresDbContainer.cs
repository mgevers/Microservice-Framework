using Testcontainers.PostgreSql;
using Xunit;

namespace Common.Testing.Integration.Containers;

public class PostgresDbContainer : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:17").Build();

    public Task InitializeAsync()
    {
        return Container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Container.StopAsync();
    }
}
