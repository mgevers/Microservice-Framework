using Common.Testing.Assert;
using Common.Testing.Persistence;

namespace Common.Testing.FluentTesting.Asserts;

public static class RepositoryAssertExtensions
{
    public static async Task<T> AssertDatabase<T>(this Task<T> resultTask, DatabaseState databaseState)
        where T : IRepsitoryTestResult
    {
        var result = await resultTask;
        AssertExtensions.EqualDatabaseStates(databaseState, result.DatabaseState);
        return result;
    }
}
