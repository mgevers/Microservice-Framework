using Xunit;
using Common.Infrastructure.Persistence;
using Common.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Common.Infrastructure.Tests.Persistence;

public class EfRepositoryTests
{
    [Fact]
    public async Task LoadAll_ReturnsAtMostCount()
    {
        using var dbContext = CreateDbContext(entityCount: 5);
        var repository = new EFRepository<Thing, ThingDbContext>(dbContext);

        var result = await repository.LoadAll(count: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task LoadAll_ReturnsAllEntities_WhenCountIsLarger()
    {
        using var dbContext = CreateDbContext(entityCount: 3);
        var repository = new EFRepository<Thing, ThingDbContext>(dbContext);

        var result = await repository.LoadAll(count: 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
    }

    private static ThingDbContext CreateDbContext(int entityCount)
    {
        var options = new DbContextOptionsBuilder<ThingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ThingDbContext(options);

        for (var i = 0; i < entityCount; i++)
        {
            dbContext.Things.Add(new Thing(Guid.NewGuid()));
        }

        dbContext.SaveChanges();
        return dbContext;
    }

    private class Thing : DataModel
    {
        public Thing(Guid id) : base(id) { }

        private Thing() { }
    }

    private class ThingDbContext : DbContext
    {
        public ThingDbContext(DbContextOptions<ThingDbContext> options) : base(options) { }

        public DbSet<Thing> Things => Set<Thing>();
    }
}
