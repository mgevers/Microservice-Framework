using Common.Infrastructure.Persistence.Cosmos;
using Common.Testing.Assert;
using Common.Testing.Integration.Containers;
using Microsoft.Azure.Cosmos;

namespace Common.Infrastructure.Functional.Tests.Persistence.Cosmos;

public class CosmosRepositoryTests : IClassFixture<CosmosContainer>
{
    private const string _databaseName = "TestDatabase";
    private const string _containerName = "PersonContainer";

    private readonly CosmosContainer cosmosContainer;

    public CosmosRepositoryTests(CosmosContainer cosmosContainer)
    {
        this.cosmosContainer = cosmosContainer;
    }

    [Fact]
    public async Task CRUD_Operations()
    {
        var repository = await CreateCosmosRepository();

        var person = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Jimmy John",
            EmailAddress = "jjohn@gmail.com",
        };

        var insertResult = await repository.Create(person);
        Assert.True(insertResult.IsSuccess);

        var findResult = await repository.LoadById(person.Id);
        Assert.True(findResult.IsSuccess);
        AssertExtensions.DeepEqual(person, findResult.Value);

        person.Name = "James John";
        var updateResult = await repository.Update(person);
        Assert.True(updateResult.IsSuccess);

        findResult = await repository.LoadById(person.Id);
        Assert.True(findResult.IsSuccess);
        AssertExtensions.DeepEqual(person, findResult.Value);

        var deleteResult = await repository.Delete(person);
        Assert.True(deleteResult.IsSuccess);

        findResult = await repository.LoadById(person.Id);
        Assert.False(findResult.IsSuccess);
    }

    [Fact]
    public async Task Query_ReadsAllPages()
    {
        var repository = await CreateCosmosRepository();

        for (var i = 0; i < 3; i++)
        {
            var createResult = await repository.Create(new Person { Id = Guid.NewGuid(), Name = "pages" });
            Assert.True(createResult.IsSuccess);
        }

        var result = await repository.Query(new QueryRequestOptions { MaxItemCount = 1 });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Count >= 3, $"expected at least 3 items, found {result.Value.Count}");
    }

    [Fact]
    public async Task LoadAll_ReturnsAtMostCount()
    {
        var repository = await CreateCosmosRepository();

        for (var i = 0; i < 3; i++)
        {
            var createResult = await repository.Create(new Person { Id = Guid.NewGuid(), Name = "load-all" });
            Assert.True(createResult.IsSuccess);
        }

        var result = await repository.LoadAll(count: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    private async Task<CosmosRepository<Person>> CreateCosmosRepository()
    {
        var cosmosClient = cosmosContainer.CosmosClient;
        var container = await EnsureCosmosContainer(cosmosClient);

        return new CosmosRepository<Person>(container);
    }

    private static async Task<Container> EnsureCosmosContainer(CosmosClient cosmosClient)
    {
        await cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
        var database = cosmosClient.GetDatabase(_databaseName);
        await database.CreateContainerIfNotExistsAsync(_containerName, "/id");

        return database.GetContainer(_containerName);
    }

    private class Person : ICosmosDataModel
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? ETag { get; set; }
    }
}
