using Ardalis.Result;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Polly;
using System.Linq.Expressions;
using System.Net;

namespace Common.Infrastructure.Persistence.Cosmos;

//TODO optimize bulk operations
public class CosmosRepository<T> : IRepository<T>
    where T : class, ICosmosDataModel
{
    private readonly Container container;

    private readonly Policy _retryPolicy = Policy.Handle<CosmosException>(ex =>
        (ex.StatusCode != HttpStatusCode.Conflict) || ex.StatusCode != HttpStatusCode.NotFound)
        .WaitAndRetry(retryCount: 4, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(250));

    private readonly Policy _circuitBreakerPolicy = Policy.Handle<CosmosException>(ex =>
        (ex.StatusCode != HttpStatusCode.Conflict) || ex.StatusCode != HttpStatusCode.NotFound)
        .CircuitBreaker(exceptionsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromSeconds(3));

    public CosmosRepository(Container container)
    {
        this.container = container;
    }

    public Task<Result<IReadOnlyList<T>>> LoadAll(int count = 1_000, CancellationToken cancellationToken = default)
    {
        var options = new QueryRequestOptions();
        options.MaxItemCount = count;

        return Query(options, cancellationToken: cancellationToken);
    }

    public async Task<Result<T>> LoadById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var idString = id.ToString();
            var response = await container.ReadItemAsync<T>(idString, new PartitionKey(idString), cancellationToken: cancellationToken);
            var entity = response.Resource;

            return Result.Success(entity);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<T>.NotFound($"could not load {typeof(T).Name} with id: '{id}'");
        }
    }

    public Task<Result<IReadOnlyList<T>>> LoadByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        return Query(filter: entity => ids.Contains(entity.Id), cancellationToken: cancellationToken);
    }

    public async virtual Task<Result<IReadOnlyList<T>>> Query(
        QueryRequestOptions? requestOptions = null,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            requestOptions ??= new QueryRequestOptions();

            // If max item count is not set, set it to -1 to return all items rather than allowing Cosmos Default to 100.
            requestOptions.MaxItemCount ??= -1;

            // Create item queryable and enforce property naming policy to use camel case.
            IQueryable<T> query = container.GetItemLinqQueryable<T>(requestOptions: requestOptions, linqSerializerOptions: new CosmosLinqSerializerOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            });

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var iterator = query.ToFeedIterator();

            var items = new List<T>();
            if (iterator.HasMoreResults)
            {
                var response = await ExecuteWithRetry(() => iterator.ReadNextAsync(cancellationToken));
                items.AddRange(response.ToList());
            }

            return items;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // If not found, return back an empty list.
            return Result<IReadOnlyList<T>>.NotFound(ex.Message);
        }
    }

    public async Task<Result<T>> Create(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await container.CreateItemAsync(
                entity,
                new PartitionKey(entity.Id.ToString()),
                cancellationToken: cancellationToken);
            var createdEntity = response.Resource;

            return Result.Success(createdEntity);
        }
        catch (CosmosException e)
        {
            return Result<T>.Error(e.Message);
        }
    }

    public async Task<Result<T>> Update(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await container.ReplaceItemAsync(
                entity,
                entity.Id.ToString(),
                new PartitionKey(entity.Id.ToString()),
                cancellationToken: cancellationToken);

            var updatedEntity = response.Resource;
            return Result.Success(updatedEntity);
        }
        catch (CosmosException e)
        {
            return Result<T>.Error(e.Message);
        }
    }

    public async Task<Result> Delete(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await container.DeleteItemAsync<T>(
                entity.Id.ToString(),
                new PartitionKey(entity.Id.ToString()),
                cancellationToken: cancellationToken);

            return Result.Success();
        }
        catch (CosmosException e)
        {
            return Result.Error(e.Message);
        }
    }

    private Task<TRet> ExecuteWithRetry<TRet>(Func<Task<TRet>> func)
    {
        return _circuitBreakerPolicy.Execute(() =>
        {
            return _retryPolicy.Execute(func);
        });
    }
}
