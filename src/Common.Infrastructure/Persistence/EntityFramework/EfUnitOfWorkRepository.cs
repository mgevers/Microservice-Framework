using Ardalis.Result;
using Microsoft.EntityFrameworkCore;

namespace Common.Infrastructure.Persistence.EntityFramework;

public class EFUnitOfWorkRepository : IUnitOfWorkRepository
{
    private readonly DbContext dbContext;

    public EFUnitOfWorkRepository(DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    async Task<Result<TEntity>> IUnitOfWorkRepository.FindById<TEntity>(Guid key, CancellationToken cancellationToken)
    {
        var entity = await this.dbContext.Set<TEntity>()
            .SingleOrDefaultAsync(p => key!.Equals(p.Id), cancellationToken);

        return entity == null
            ? Result<TEntity>.NotFound($"entity with id '{key}' not found")
            : Result.Success(entity);
    }

    void IUnitOfWorkRepository.Add<TEntity>(TEntity entity)
    {
        dbContext.Add(entity);
    }

    void IUnitOfWorkRepository.AddMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        dbContext.AddRange(entities);
    }

    void IUnitOfWorkRepository.Remove<TEntity>(TEntity entity)
    {
        dbContext.Remove(entity);
    }

    void IUnitOfWorkRepository.RemoveMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        dbContext.RemoveRange(entities);
    }

    void IUnitOfWorkRepository.Update<TEntity>(TEntity entity)
    {
        dbContext.Update(entity);
    }

    void IUnitOfWorkRepository.UpdateMany<TEntity>(IReadOnlyCollection<TEntity> entities)
    {
        dbContext.UpdateRange(entities);
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
