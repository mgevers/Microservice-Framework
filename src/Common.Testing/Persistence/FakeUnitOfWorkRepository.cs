using Ardalis.Result;
using Common.LanguageExtensions.Contracts;
using Common.LanguageExtensions.Utilities;
using System.Linq.Expressions;

namespace Common.Testing.Persistence;

public class FakeUnitOfWorkRepository : IUnitOfWorkRepository
{
    private readonly List<IDataModel> toAdd = [];
    private readonly List<IDataModel> toUpdate = [];
    private readonly List<IDataModel> toRemove = [];

    Task<Result<TEntity>> IReadOnlyRepository.Find<TEntity>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
    {
        var match = FakeDatabase.Query(queryFunc)
            .SingleOrDefault();

        var result = match != null
            ? Result<TEntity>.Success(match)
            : Result<TEntity>.NotFound();

        return Task.FromResult(result);
    }

    Task<Result<IReadOnlyList<TEntity>>> IReadOnlyRepository.LoadAll<TEntity>(int count, CancellationToken cancellationToken)
    {
        var matches = FakeDatabase.Query<TEntity>().ToList();

        var result = matches != null
            ? Result<IReadOnlyList<TEntity>>.Success(matches)
            : Result<IReadOnlyList<TEntity>>.NotFound();

        return Task.FromResult(result);
    }

    Task<Result<TEntity>> IReadOnlyRepository.LoadById<TEntity>(Guid id, CancellationToken cancellationToken)
    {
        var match = FakeDatabase.Query<TEntity>(entity => entity.Id == id)
            .SingleOrDefault();

        var result = match != null
            ? Result.Success(match)
            : Result<TEntity>.NotFound($"no saved entity with id: '{id}'");

        return Task.FromResult(result);
    }

    Task<Result<IReadOnlyList<TEntity>>> IReadOnlyRepository.LoadByIds<TEntity>(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var matches = FakeDatabase.Query<TEntity>(entity => ids.Contains(entity.Id))
            .ToList();

        var result = matches != null
            ? Result<IReadOnlyList<TEntity>>.Success(matches)
            : Result<IReadOnlyList<TEntity>>.NotFound();

        return Task.FromResult(result);
    }

    Task<Result<IReadOnlyList<TEntity>>> IReadOnlyRepository.Query<TEntity>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
    {
        var matches = FakeDatabase.Query(queryFunc)
            .ToList();

        var result = matches != null
            ? Result<IReadOnlyList<TEntity>>.Success(matches)
            : Result<IReadOnlyList<TEntity>>.NotFound();

        return Task.FromResult(result);
    }

    void IUnitOfWorkRepository.Add<TEntity>(TEntity entity)
    {
        this.toAdd.Add(entity);
    }

    void IUnitOfWorkRepository.AddMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        this.toAdd.AddRange(entities);
    }

    void IUnitOfWorkRepository.Remove<TEntity>(TEntity entity)
    {
        this.toRemove.Add(entity);
    }

    void IUnitOfWorkRepository.RemoveMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        this.toRemove.AddRange(entities);
    }

    void IUnitOfWorkRepository.Update<TEntity>(TEntity entity)
    {
        this.toUpdate.Add(entity);
    }

    void IUnitOfWorkRepository.UpdateMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        this.toUpdate.AddRange(entities);
    }

    public Task<Result> CommitTransaction(CancellationToken cancellation = default)
    {
        var addResults = this.toAdd
            .Select(entity => FakeDatabase.InsertEntity(entity).AsResult())
            .ToList();
        var updateResults = this.toUpdate
            .Select(entity => FakeDatabase.UpdateEntity(entity).AsResult())
            .ToList();
        var removeResults = this.toRemove
            .Select(entity => FakeDatabase.DeleteEntity(entity))
            .ToList();

        var allResults = addResults.Concat(updateResults).Concat(removeResults);

        return Task.FromResult(allResults.Combine());
    }
}
