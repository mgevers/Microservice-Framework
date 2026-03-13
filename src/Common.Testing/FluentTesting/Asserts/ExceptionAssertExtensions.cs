namespace Common.Testing.FluentTesting.Asserts;

public static class ExceptionAssertExtensions
{
    public static async Task<T> AssertExceptionThrown<T>(this Task<T> resultTask, Type? exceptionType = null)
        where T : IExceptionTestResult
    {
        exceptionType ??= typeof(Exception);
        var result = await resultTask;

        var exception = result.ExceptionThrown;

        Xunit.Assert.NotNull(exception);
        Xunit.Assert.True(
            exceptionType.IsAssignableFrom(exception.GetType()),
            $"{exceptionType.Name} not assignable from {exception.GetType().Name}");

        return result;
    }
}
