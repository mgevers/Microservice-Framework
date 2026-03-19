using Ardalis.Result;
using Common.LanguageExtensions.Contracts;
using Common.LanguageExtensions.Utilities.ResultExtensions;
using System.Linq.Expressions;

namespace Common.Testing.Persistence;

public class FakeUnitOfWorkRepository : IUnitOfWorkRepository
{
    private readonly List<IDataModelBase> toAdd = [];
    private readonly List<IDataModelBase> toUpdate = [];
    private readonly List<IDataModelBase> toRemove = [];

    public Task<Result<TEntity>> Find<TEntity, TKey>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
        where TEntity : class, IDataModel<TKey>
    {
        var match = FakeDatabase.Query(queryFunc)
            .SingleOrDefault();

        var result = match != null
            ? Result<TEntity>.Success(match)
            : Result<TEntity>.NotFound();

        return Task.FromResult(result);
    }

    public Task<Result<TEntity>> Find<TEntity>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
        where TEntity : class, IDataModel
    {
        return this.Find<TEntity, Guid>(queryFunc, cancellationToken);
    }

    public Task<Result<IReadOnlyList<TEntity>>> LoadAll<TEntity, TKey>(int count, CancellationToken cancellationToken)
        where TEntity : class, IDataModel<TKey>
    {
        var matches = FakeDatabase.Query<TEntity>().ToList();

        var result = matches != null
            ? Result<IReadOnlyList<TEntity>>.Success(matches)
            : Result<IReadOnlyList<TEntity>>.NotFound();

        return Task.FromResult(result);
    }

    public Task<Result<IReadOnlyList<TEntity>>> LoadAll<TEntity>(int count, CancellationToken cancellationToken)
        where TEntity : class, IDataModel
    {
        return this.LoadAll<TEntity, Guid>(count, cancellationToken);
    }

    public Task<Result<TEntity>> LoadById<TEntity, TKey>(TKey id, CancellationToken cancellationToken)
        where TEntity : class, IDataModel<TKey>
    {
        var match = FakeDatabase.Query<TEntity>(entity => entity.Id!.Equals(id))
            .SingleOrDefault();

        var result = match != null
            ? Result.Success(match)
            : Result<TEntity>.NotFound($"no saved entity with id: '{id}'");

        return Task.FromResult(result);
    }

    public Task<Result<TEntity>> LoadById<TEntity>(Guid id, CancellationToken cancellationToken)
        where TEntity : class, IDataModel
    {
        return this.LoadById<TEntity, Guid>(id, cancellationToken);
    }

    public Task<Result<IReadOnlyList<TEntity>>> LoadByIds<TEntity, TKey>(IReadOnlyCollection<TKey> ids, CancellationToken cancellationToken)
        where TEntity : class, IDataModel<TKey>
    {
        var matches = FakeDatabase.Query<TEntity>(entity => ids.Contains(entity.Id))
            .ToList();

        var result = matches != null
            ? Result<IReadOnlyList<TEntity>>.Success(matches)
            : Result<IReadOnlyList<TEntity>>.NotFound();

        return Task.FromResult(result);
    }

    public Task<Result<IReadOnlyList<TEntity>>> LoadByIds<TEntity>(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        where TEntity : class, IDataModel
    {
        return this.LoadByIds<TEntity, Guid>(ids, cancellationToken);
    }

    public Task<Result<IReadOnlyList<TEntity>>> Query<TEntity, TKey>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
        where TEntity : class, IDataModel<TKey>
    {
        var matches = FakeDatabase.Query(queryFunc)
            .ToList();

        var result = matches != null
            ? Result<IReadOnlyList<TEntity>>.Success(matches)
            : Result<IReadOnlyList<TEntity>>.NotFound();

        return Task.FromResult(result);
    }

    public Task<Result<IReadOnlyList<TEntity>>> Query<TEntity>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
        where TEntity : class, IDataModel
    {
        return this.Query<TEntity, Guid>(queryFunc, cancellationToken);
    }

    public void Add<TEntity>(TEntity entity)
        where TEntity : class, IDataModelBase
    {
        this.toAdd.Add(entity);
    }

    public void AddMany<TEntity>(IReadOnlyCollection<TEntity> entities)
        where TEntity : class, IDataModelBase
    {
        this.toAdd.AddRange(entities);
    }

    public void Remove<TEntity>(TEntity entity)
        where TEntity : class, IDataModelBase
    {
        this.toRemove.Add(entity);
    }

    public void RemoveMany<TEntity>(IReadOnlyCollection<TEntity> entities)
        where TEntity : class, IDataModelBase
    {
        this.toRemove.AddRange(entities);
    }

    public void Update<TEntity>(TEntity entity)
        where TEntity : class, IDataModelBase
    {
        this.toUpdate.Add(entity);
    }

    public void UpdateMany<TEntity>(IReadOnlyCollection<TEntity> entities)
        where TEntity : class, IDataModelBase
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
