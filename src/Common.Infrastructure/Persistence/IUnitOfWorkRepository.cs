using Ardalis.Result;

namespace Common.Infrastructure.Persistence;

public interface IUnitOfWorkRepository
{
    Task<Result<TEntity>> FindById<TEntity>(Guid key, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel;
    public void Add<TEntity>(TEntity entity)
        where TEntity : class, IDataModel;
    public void AddMany<TEntity>(IReadOnlyCollection<TEntity> entities)
        where TEntity : class, IDataModel;
    public void Update<TEntity>(TEntity entity)
        where TEntity : class, IDataModel;
    public void UpdateMany<TEntity>(IReadOnlyCollection<TEntity> entities)
        where TEntity : class, IDataModel;
    public void Remove<TEntity>(TEntity entity)
        where TEntity : class, IDataModel;
    public void RemoveMany<TEntity>(IReadOnlyCollection<TEntity> entities)
        where TEntity : class, IDataModel;
    public Task<Result> CommitTransaction(CancellationToken cancellation = default);
}
