using Common.Infrastructure.Persistence;
using Common.Infrastructure.Persistence.Elasticsearch;
using Common.Testing.Assert;
using Common.Testing.Integration.Containers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;

namespace Common.Infrastructure.Functional.Tests.Persistence.Cosmos;

public class ElasticSearchRepositoryTests : IClassFixture<ElasticsearchContainer>
{
    private readonly ElasticsearchContainer _elasticSearchContainer;

    public ElasticSearchRepositoryTests(ElasticsearchContainer elasticSearchContainer)
    {
        this._elasticSearchContainer = elasticSearchContainer;
    }

    [Fact]
    public async Task CRUD_Operations()
    {
        var repository = await CreateElasticSearchRepository();

        var person = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Jimmy John",
            EmailAddress = "jjohn@gmail.com",
        };

        var insertResult = await repository.Create(person);
        Assert.True(insertResult.IsSuccess, string.Join(',', insertResult.Errors));

        var findResult = await repository.LoadById(person.Id);
        Assert.True(findResult.IsSuccess, string.Join(',', findResult.Errors));
        AssertExtensions.DeepEqual(person, findResult.Value);

        person.Name = "James John";
        var updateResult = await repository.Update(person);
        Assert.True(updateResult.IsSuccess, string.Join(',', updateResult.Errors));

        findResult = await repository.LoadById(person.Id);
        Assert.True(findResult.IsSuccess, string.Join(',', findResult.Errors));
        AssertExtensions.DeepEqual(person, findResult.Value);

        var deleteResult = await repository.Delete(person);
        Assert.True(deleteResult.IsSuccess, string.Join(',', deleteResult.Errors));

        findResult = await repository.LoadById(person.Id);
        Assert.False(findResult.IsSuccess, string.Join(',', findResult.Errors));
    }

    [Fact]
    public async Task LoadByIds()
    {
        var person1Id = Guid.Parse("bab52b89-c496-4c17-b4b5-08ae72049b6c");
        var person2Id = Guid.Parse("cd8f4fe6-1b2d-417e-8e96-feb2edc5aa5c");

        var repository = await CreateElasticSearchRepository();

        var person1 = new Person
        {
            Id = person1Id,
            Name = "Jimmy John",
            EmailAddress = "jjohn@gmail.com",
        };
        var person2 = new Person
        {
            Id = person2Id,
            Name = "James Bond",
            EmailAddress = "jbond@gmail.com",
        };

        var insert1Result = await repository.Create(person1);
        Assert.True(insert1Result.IsSuccess, string.Join(',', insert1Result.Errors));

        var insert2Result = await repository.Create(person2);
        Assert.True(insert2Result.IsSuccess, string.Join(',', insert2Result.Errors));

        await Task.Delay(1000);

        var loadByIdsResult = await repository.LoadByIds([person1Id, person2Id]);
        Assert.True(loadByIdsResult.IsSuccess, string.Join(',', loadByIdsResult.Errors));
        Assert.True(loadByIdsResult.Value.Count == 2, $"expected 2 people to be returned, but got {loadByIdsResult.Value.Count}");
    }

    [Fact]
    public async Task LoadAll()
    {
        var repository = await CreateElasticSearchRepository();

        var people = new Person[]
        {
            new Person
            {
                Id = Guid.NewGuid(),
                Name = "Jimmy John",
                EmailAddress = "jjohn@gmail.com",
            },
            new Person
            {
                Id = Guid.NewGuid(),
                Name = "James Bond",
                EmailAddress = "jbond@gmail.com",
            },
            new Person
            {
                Id = Guid.NewGuid(),
                Name = "Jimmy Bond",
                EmailAddress = "jimmyBond@gmail.com",
            }
        };

        var insertResult = await repository.CreateMany(people);
        Assert.True(insertResult.IsSuccess, string.Join(',', insertResult.Errors));

        await Task.Delay(1000);

        var loadAllResult = await repository.LoadAll();
        Assert.True(loadAllResult.IsSuccess, string.Join(',', loadAllResult.Errors));
        Assert.True(loadAllResult.Value.Count >= 3, $"expected at least 3 people to be returned, but got {loadAllResult.Value.Count}");
    }

    private async Task<ElasticsearchRepository<Person>> CreateElasticSearchRepository()
    {
        var client = _elasticSearchContainer.ElasticsearchClient;
        await EnsureIndexCreated(client);

        return new ElasticsearchRepository<Person>(client, "person");
    }

    private static async Task EnsureIndexCreated(ElasticsearchClient client)
    {
        var indexName = "person";
        var getRequest = new GetIndexRequest(indexName);
        var getResponse = await client.Indices.GetAsync(getRequest);

        if (getResponse.Indices.Keys.Contains(indexName))
        {
            return;
        }

        var request = new CreateIndexRequestDescriptor<Person>("person");
        var response = await client.Indices.CreateAsync(request);

        if (response.IsSuccess() == false)
        {
            throw new Exception("can't create index");
        }
    }

    private class Person : IDataModel
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? ETag { get; set; }
    }
}
