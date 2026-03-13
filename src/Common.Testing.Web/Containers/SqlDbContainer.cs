using Testcontainers.MsSql;
using Xunit;

namespace Common.Testing.Integration.Containers;

public class SqlDbContainer : IAsyncLifetime
{
    public MsSqlContainer Container { get; } = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Container.StopAsync();
    }
}
