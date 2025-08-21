using Microsoft.EntityFrameworkCore;

namespace Common.Testing.Persistence
{
    public static class DbContextExtensions
    {
        public static async Task SeedData(this DbContext dbContext, DatabaseState databaseState)
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.AddRange(databaseState.GetAllEntities());
            await dbContext.SaveChangesAsync();
        }
    }
}
