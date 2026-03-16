using Common.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Testcontainers.CosmosDb;
using Xunit;

namespace Common.Testing.Integration.Containers;

public class CosmosContainer : IAsyncLifetime
{
    public CosmosDbContainer CosmosDbContainer { get; } = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
        .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "10")
        .WithEnvironment("AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE", "127.0.0.1")
        .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "false")
        .WithPortBinding(8081, 8081)
        .Build();

    private CosmosClient? _cosmosClient;

    public CosmosClient CosmosClient
    {
        get
        {
            if (_cosmosClient == null)
            {
                _cosmosClient = new CosmosClientBuilder(CosmosDbContainer.GetConnectionString())
                    .WithCustomSerializer(new CosmosDbSerializer())
                    .WithConnectionModeGateway()
                    .WithRequestTimeout(TimeSpan.FromSeconds(120))
                    .WithThrottlingRetryOptions(TimeSpan.FromSeconds(10), 10)
                    .Build();
            }
            return _cosmosClient;
        }
    }

    public async ValueTask InitializeAsync()
    {
        await CosmosDbContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _cosmosClient?.Dispose();
        await CosmosDbContainer.StopAsync();
    }
}
