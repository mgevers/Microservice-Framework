using Ardalis.Result;
using Common.LanguageExtensions.Contracts;

namespace Common.Testing.Persistence;

public class FakeRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : IDataModel<TKey>
{
    public IReadOnlyList<TEntity> Entities => FakeDatabase.Query<TEntity>();

    public Task<Result<IReadOnlyList<TEntity>>> LoadAll(int count = 1000, CancellationToken cancellationToken = default)
    {
        var matches = FakeDatabase.Query<TEntity>().ToList();

        var result = matches != null
            ? Result<IReadOnlyList<TEntity>>.Success(matches)
            : Result<IReadOnlyList<TEntity>>.NotFound();

        return Task.FromResult(result);
    }

    public Task<Result<TEntity>> LoadById(TKey id, CancellationToken cancellationToken = default)
    {
        var match = FakeDatabase.Query<TEntity>(entity => entity.Id!.Equals(id))
            .SingleOrDefault();

        var result = match != null
            ? Result.Success(match)
            : Result<TEntity>.NotFound($"no saved entity with id: '{id}'");

        return Task.FromResult(result);
    }

    public Task<Result<IReadOnlyList<TEntity>>> LoadByIds(IReadOnlyCollection<TKey> ids, CancellationToken cancellationToken = default)
    {
        var matches = FakeDatabase.Query<TEntity>(entity => ids.Contains(entity.Id))
            .ToList();

        var result = matches != null
            ? Result<IReadOnlyList<TEntity>>.Success(matches)
            : Result<IReadOnlyList<TEntity>>.NotFound();

        return Task.FromResult(result);
    }

    public Task<Result<TEntity>> Create(TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FakeDatabase.InsertEntity(entity));
    }

    public Task<Result<TEntity>> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FakeDatabase.UpdateEntity(entity));
    }

    public Task<Result> Delete(TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FakeDatabase.DeleteEntity(entity));
    }
}

public class FakeRepository<TEntity> : FakeRepository<TEntity, Guid>, IRepository<TEntity>
    where TEntity : IDataModel
{
}
