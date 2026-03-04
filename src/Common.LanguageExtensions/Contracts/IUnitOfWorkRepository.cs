using Ardalis.Result;
using System.Linq.Expressions;

namespace Common.LanguageExtensions.Contracts;

public interface IReadOnlyRepository
{
    Task<Result<IReadOnlyList<TEntity>>> LoadAll<TEntity>(int count = 1_000, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel;

    Task<Result<TEntity>> LoadById<TEntity>(Guid id, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel;
    Task<Result<IReadOnlyList<TEntity>>> LoadByIds<TEntity>(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel;

    Task<Result<IReadOnlyList<TEntity>>> Query<TEntity>(
        Expression<Func<TEntity, bool>> queryFunc,
        CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel;

    Task<Result<TEntity>> Find<TEntity>(
        Expression<Func<TEntity, bool>> queryFunc,
        CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel;
}

public interface IUnitOfWorkRepository : IReadOnlyRepository
{        
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
