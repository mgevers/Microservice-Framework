using Ardalis.Result;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using A = Ardalis.Result;

namespace Common.Infrastructure.Persistence.Elasticsearch;

public class ElasticsearchRepository<TEntity> : IElasticsearchRepository<TEntity>
    where TEntity : class, IDataModel
{
    private readonly ElasticsearchClient client;

    public ElasticsearchRepository(ElasticsearchClient client, string indexName)
    {
        this.client = client;
        IndexName = indexName;
    }

    public string IndexName { get; }

    public async Task<Result<IReadOnlyList<TEntity>>> Search(SearchRequest request)
    {
        var response = await this.client.SearchAsync<TEntity>(request);
        var debug = response.DebugInformation;

        if (response == null || response.IsSuccess() == false)
        {
            return Result<IReadOnlyList<TEntity>>.CriticalError(response?.DebugInformation ?? "elasticsearch response  came back null");
        }

        var matches = response.Documents.ToList();
        return Result<IReadOnlyList<TEntity>>.Success(matches);
    }

    public async Task<A.Result> CreateMany(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var response = await client.IndexManyAsync(entities, IndexName, cancellationToken);

        if (response.IsSuccess())
        {
            return A.Result.Success();
        }

        return A.Result.Error(response.DebugInformation);
    }

    public async Task<Result<IReadOnlyList<TEntity>>> LoadAll(int count = 1000, CancellationToken cancellationToken = default)
    {
        var searchRequest = new SearchRequest(IndexName)
        {
            From = 0,
            Size = count,
            Query = new MatchAllQuery(),
        };

        var response = await client.SearchAsync<TEntity>(searchRequest, cancellationToken);

        if (response == null || response.IsSuccess() == false)
        {
            return Result<IReadOnlyList<TEntity>>.Error(response?.DebugInformation ?? "elasticsearch response  came back null");
        }

        var matches = response.Documents.ToList();

        return Result<IReadOnlyList<TEntity>>.Success(matches);
    }

    public async Task<Result<TEntity>> LoadById(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await this.client.GetAsync<TEntity>(IndexName, id, cancellationToken);

        if (response.IsSuccess())
        {
            if (response.Source == null)
            {
                return A.Result<TEntity>.NotFound($"key:'{id}' not found");
            }

            return A.Result<TEntity>.Success(response.Source);
        }

        return A.Result<TEntity>.Error(response.ElasticsearchServerError?.Error.Reason);
    }

    public async Task<Result<IReadOnlyList<TEntity>>> LoadByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        List<string> stringIds = ids.Select(id => id.ToString()).ToList();
        var terms = stringIds.Select(id => (FieldValue)id).ToList();

        var searchRequest = new SearchRequest(IndexName)
        {
            From = 0,
            Size = 10_000, 
            Query = new TermsQuery()
            {
                Field = "_id"!,
                Terms = new TermsQueryField(terms),
            },
        };        

        var response = await client.SearchAsync<TEntity>(searchRequest, cancellationToken);

        if (response == null || response.IsSuccess() == false)
        {
            return Result<IReadOnlyList<TEntity>>.Error(response?.DebugInformation ?? "elasticsearch response  came back null");
        }

        var matches = response.Documents.ToList();

        return Result<IReadOnlyList<TEntity>>.Success(matches);
    }

    public async Task<Result<TEntity>> Create(TEntity entity, CancellationToken cancellationToken = default)
    {
        var request = new IndexRequest<TEntity>(entity, index: IndexName);
        var response = await client.IndexAsync(request, cancellationToken);

        if (response.IsSuccess())
        {
            return A.Result.Success(entity);
        }

        return A.Result<TEntity>.Error(response.DebugInformation);
    }

    public async Task<Result<TEntity>> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        var response = await client.UpdateAsync<TEntity, TEntity>(IndexName, entity.Id, u => u.Doc(entity), cancellationToken);

        if (response.IsSuccess())
        {
            return A.Result.Success(entity);
        }

        return A.Result<TEntity>.Error(response.DebugInformation);
    }

    public async Task<A.Result> Delete(TEntity entity, CancellationToken cancellationToken = default)
    {
        var request = new DeleteRequest(IndexName, entity.Id);
        var response = await client.DeleteAsync(request, cancellationToken);

        if (response.IsSuccess())
        {
            return A.Result.Success();
        }

        return A.Result.Error(response.DebugInformation);
    }
}
