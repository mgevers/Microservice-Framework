using Ardalis.Result;

namespace Common.Infrastructure.Persistence
{
    public interface IRepository<TEntity> where TEntity : IDataModel
    {
        Task<Result<IReadOnlyList<TEntity>>> LoadAll(int count = 1_000, CancellationToken cancellationToken = default);

        Task<Result<TEntity>> LoadById(Guid id, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<TEntity>>> LoadByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

        Task<Result<TEntity>> Create(TEntity entity, CancellationToken cancellationToken = default);

        Task<Result<TEntity>> Update(TEntity entity, CancellationToken cancellationToken = default);

        Task<Result> Delete(TEntity entity, CancellationToken cancellationToken = default);
    }
}
