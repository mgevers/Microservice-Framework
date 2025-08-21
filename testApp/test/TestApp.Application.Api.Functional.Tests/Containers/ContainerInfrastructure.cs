using Testcontainers.MsSql;

namespace TestApp.Application.Api.Functional.Tests.Containers;

public class ContainerInfrastructure
{
    public MsSqlContainer SqlContainer { get; } = new MsSqlBuilder().Build();
}
