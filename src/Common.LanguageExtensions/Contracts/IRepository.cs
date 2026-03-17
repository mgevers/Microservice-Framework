using Ardalis.Result;

namespace Common.LanguageExtensions.Contracts;

public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : IDataModel<TKey>
{
    Task<Result<IReadOnlyList<TEntity>>> LoadAll(int count = 1_000, CancellationToken cancellationToken = default);
    Task<Result<TEntity>> LoadById(TKey id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TEntity>>> LoadByIds(IReadOnlyCollection<TKey> ids, CancellationToken cancellationToken = default);
}

public interface IReadOnlyRepository<TEntity> : IReadOnlyRepository<TEntity, Guid>
    where TEntity : IDataModel
{
}

public interface IRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : IDataModel<TKey>
{
    Task<Result<TEntity>> Create(TEntity entity, CancellationToken cancellationToken = default);

    Task<Result<TEntity>> Update(TEntity entity, CancellationToken cancellationToken = default);

    Task<Result> Delete(TEntity entity, CancellationToken cancellationToken = default);
}

public interface IRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : IDataModel
{
}
