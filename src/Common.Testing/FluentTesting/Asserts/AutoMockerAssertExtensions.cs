using Moq.AutoMock;

namespace Common.Testing.FluentTesting.Asserts;

public static class AutoMockerAssertExtensions
{
    public static async Task<T> AssertMocker<T>(this Task<T> resultTask, Action<AutoMocker> action)
        where T : IAutoMockerTestResult
    {
        var result = await resultTask;

        action(result.AutoMocker);

        return result;
    }
}
