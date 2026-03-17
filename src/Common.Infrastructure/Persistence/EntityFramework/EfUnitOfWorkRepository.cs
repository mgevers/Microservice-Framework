using Ardalis.Result;
using Common.LanguageExtensions.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Common.Infrastructure.Persistence.EntityFramework;

public class EFUnitOfWorkRepository : IUnitOfWorkRepository
{
    private readonly DbContext dbContext;

    public EFUnitOfWorkRepository(DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<TEntity>>> LoadAll<TEntity, TKey>(int count = 1_000, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel<TKey>
    {
        var entity = await this.dbContext.Set<TEntity>()
            .ToListAsync(cancellationToken);

        return entity == null
            ? Result<IReadOnlyList<TEntity>>.NotFound($"entity with id not found")
            : Result.Success<IReadOnlyList<TEntity>>(entity);
    }

    public Task<Result<IReadOnlyList<TEntity>>> LoadAll<TEntity>(int count = 1_000, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel
    {
        return this.LoadAll<TEntity, Guid>(count, cancellationToken);
    }

    public async Task<Result<TEntity>> LoadById<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel<TKey>
    {
        var entity = await this.dbContext.Set<TEntity>()
            .SingleOrDefaultAsync(p => id!.Equals(p.Id), cancellationToken);

        return entity == null
            ? Result<TEntity>.NotFound($"entity with id '{id}' not found")
            : Result.Success(entity);
    }

    public Task<Result<TEntity>> LoadById<TEntity>(Guid id, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel
    {
        return this.LoadById<TEntity, Guid>(id, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<TEntity>>> LoadByIds<TEntity, TKey>(IReadOnlyCollection<TKey> ids, CancellationToken cancellationToken = default) 
        where TEntity : class, IDataModel<TKey>
    {
        var entities = await this.dbContext.Set<TEntity>()
            .Where(entity => ids.Contains(entity.Id))
            .ToListAsync(cancellationToken);

        return entities == null
            ? Result<IReadOnlyList<TEntity>>.NotFound($"entity with id not found")
            : Result.Success<IReadOnlyList<TEntity>>(entities);
    }

    public Task<Result<IReadOnlyList<TEntity>>> LoadByIds<TEntity>(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        where TEntity : class, IDataModel
    {
        return this.LoadByIds<TEntity, Guid>(ids, cancellationToken);
    }

    async Task<Result<IReadOnlyList<TEntity>>> IReadOnlyRepository.Query<TEntity, TKey>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
    {
        var entities = await this.dbContext.Set<TEntity>()
            .Where(queryFunc)
            .ToListAsync(cancellationToken);

        return entities == null
            ? Result<IReadOnlyList<TEntity>>.NotFound($"entity with id not found")
            : Result.Success<IReadOnlyList<TEntity>>(entities);
    }

    async Task<Result<IReadOnlyList<TEntity>>> IReadOnlyRepository.Query<TEntity>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
    {
        var entities = await this.dbContext.Set<TEntity>()
            .Where(queryFunc)
            .ToListAsync(cancellationToken);

        return entities == null
            ? Result<IReadOnlyList<TEntity>>.NotFound($"entity with id not found")
            : Result.Success<IReadOnlyList<TEntity>>(entities);
    }

    async Task<Result<TEntity>> IReadOnlyRepository.Find<TEntity, TKey>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
    {
        var entity = await this.dbContext.Set<TEntity>()
            .SingleOrDefaultAsync(queryFunc, cancellationToken);

        return entity == null
            ? Result<TEntity>.NotFound($"entity not found")
            : Result.Success<TEntity>(entity);
    }

    async Task<Result<TEntity>> IReadOnlyRepository.Find<TEntity>(Expression<Func<TEntity, bool>> queryFunc, CancellationToken cancellationToken)
    {
        var entity = await this.dbContext.Set<TEntity>()
            .SingleOrDefaultAsync(queryFunc, cancellationToken);

        return entity == null
            ? Result<TEntity>.NotFound($"entity not found")
            : Result.Success<TEntity>(entity);
    }

    void IUnitOfWorkRepository.Add<TEntity>(TEntity entity)
    {
        this.dbContext.Add(entity);
    }

    void IUnitOfWorkRepository.AddMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        this.dbContext.AddRange(entities);
    }

    void IUnitOfWorkRepository.Update<TEntity>(TEntity entity)
    {
        this.dbContext.Update(entity);
    }

    void IUnitOfWorkRepository.UpdateMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        this.dbContext.UpdateRange(entities);
    }

    void IUnitOfWorkRepository.Remove<TEntity>(TEntity entity)
    {
        this.dbContext.Remove(entity);
    }

    void IUnitOfWorkRepository.RemoveMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        this.dbContext.RemoveRange(entities);
    }

    public async Task<Result> CommitTransaction(CancellationToken cancellation = default)
    {
        try
        {
            var count = await this.dbContext.SaveChangesAsync(cancellation);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Conflict($"conflict - {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.CriticalError(ex.Message);
        }
    }
}
