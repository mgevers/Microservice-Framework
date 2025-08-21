using Common.Infrastructure.Persistence.Cosmos;
using DotNet.Testcontainers.Builders;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Testcontainers.CosmosDb;
using Xunit;

namespace Common.Testing.Integration.Containers;

public class CosmosContainer : IAsyncLifetime
{
    public CosmosDbContainer CosmosDbContainer { get; } = new CosmosDbBuilder()
        .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
        .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "10")
        .WithCommand("--protocol", "https", "--enable-explorer", "true")
        .WithWaitStrategy(Wait
            .ForUnixContainer()
            .UntilPortIsAvailable(8081)
        )
        .Build();

    public CosmosClient CosmosClient => new CosmosClientBuilder(CosmosDbContainer.GetConnectionString())
            .WithCustomSerializer(new CosmosDbSerializer())
            .WithHttpClientFactory(() => CosmosDbContainer.HttpClient)
            .WithConnectionModeGateway()
            .Build();

    public Task DisposeAsync()
    {
        return CosmosDbContainer.StopAsync();
    }

    public Task InitializeAsync()
    {
        return CosmosDbContainer.StartAsync();
    }
}
