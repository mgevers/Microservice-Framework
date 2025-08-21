using Common.Infrastructure.Persistence.EntityFramework.ModelBuilding;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Common.Infrastructure.Persistence.EntityFramework
{
    public abstract class CommonDbContext : DbContext
    {
        protected CommonDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public async Task<IReadOnlyList<TView>> Query<TEntity, TView>(
            Expression<Func<TEntity, bool>> filter,
            Expression<Func<TEntity, TView>> projection,
            CancellationToken cancellationToken)
            where TEntity : Entity<Guid>
        {
            return (await this.Set<TEntity>()
                .AsNoTracking()
                .Where(filter)
                .Select(projection)
                .ToListAsync(cancellationToken)).AsReadOnly();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .RegisterValueObjectsAsOwnedEntities()
                .RegisterDomainEventsAsOwnedEntities();
        }
    }
}
