using Ardalis.Result;
using Microsoft.EntityFrameworkCore;

namespace Common.Infrastructure.Persistence.EntityFramework;

public class EFRepository<TEntity, TDbContext> : IRepository<TEntity>
    where TEntity : class, IDataModel
    where TDbContext : DbContext
{
    protected readonly TDbContext dbContext;

    public EFRepository(TDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async virtual Task<Result<IReadOnlyList<TEntity>>> LoadAll(int count = 1_000, CancellationToken cancellationToken = default)
    {
        var entities = await this.dbContext.Set<TEntity>()
            .ToListAsync(cancellationToken);

        return entities == null || entities.Count == 0
            ? Result<IReadOnlyList<TEntity>>.NotFound()
            : Result<IReadOnlyList<TEntity>>.Success(entities);
    }

    public async virtual Task<Result<TEntity>> LoadById(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await this.dbContext.Set<TEntity>()
            .SingleOrDefaultAsync(p => id!.Equals(p.Id), cancellationToken);

        return entity == null
            ? Result<TEntity>.NotFound($"entity with id '{id}' not found")
            : Result<TEntity>.Success(entity);
    }

    public async virtual Task<Result<IReadOnlyList<TEntity>>> LoadByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var entities = await this.dbContext.Set<TEntity>()
            .Where(entity => ids.Contains(entity.Id))
            .ToListAsync(cancellationToken);

        return entities == null || entities.Count == 0
            ? Result<IReadOnlyList<TEntity>>.NotFound()
            : Result<IReadOnlyList<TEntity>>.Success(entities);
    }

    public async Task<Result<TEntity>> Create(TEntity entity, CancellationToken cancellationToken = default)
    {
        dbContext.Add(entity);
        try
        {
            var count = await this.dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success(entity);
        }
        catch (DbUpdateException ex)
        {
            return Result<TEntity>.Conflict($"conflict - {ex.Message}");
        }
    }

    public async Task<Result> Delete(TEntity entity, CancellationToken cancellationToken = default)
    {
        dbContext.Remove(entity);
        try
        {
            var count = await this.dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Conflict($"conflict - {ex.Message}");
        }
    }

    public async Task<Result<TEntity>> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        dbContext.Update(entity);
        try
        {
            var count = await this.dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success(entity);
        }
        catch (DbUpdateException ex)
        {
            return Result<TEntity>.Conflict($"conflict - {ex.Message}");
        }
    }
}
