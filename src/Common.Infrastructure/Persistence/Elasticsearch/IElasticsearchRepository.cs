using Ardalis.Result;
using Elastic.Clients.Elasticsearch;
using A = Ardalis.Result;

namespace Common.Infrastructure.Persistence.Elasticsearch;

public interface IElasticsearchRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, IDataModel
{
    Task<Result<IReadOnlyList<TEntity>>> Search(SearchRequest request);
    Task<A.Result> CreateMany(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default);
}
