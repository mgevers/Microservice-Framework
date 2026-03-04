using Testcontainers.MsSql;

namespace TestApp.Application.Api.Functional.Tests.Containers;

public class ContainerInfrastructure
{
    public MsSqlContainer SqlContainer { get; } = new MsSqlBuilder("mysql:8.0.45-debian").Build();
}
