using Common.Testing.Assert;
using CSharpFunctionalExtensions;

namespace Common.Testing.FluentTesting.Asserts;

public static class OutputAssertExtensions
{
    public static async Task<T> AssertOutput<T>(this Task<T> resultTask, Result expectedResult)
        where T : ITestOutput<Result>
    {
        var result = await resultTask;

        AssertExtensions.EqualResults(expectedResult, result.Output);

        return result;
    }

    public static async Task<T> AssertOutput<T, TOutput>(this Task<T> resultTask, TOutput expectedResult)
        where T : ITestOutput<TOutput>
    {
        var result = await resultTask;

        AssertExtensions.DeepEqual(expectedResult, result.Output);

        return result;
    }

    public static async Task<T> AssertHttpResponse<T>(this Task<T> resultTask, Func<HttpResponseMessage, bool> func)
        where T : ITestOutput<HttpResponseMessage>
    {
        var result = await resultTask;

        var output = func(result.Output);

        Xunit.Assert.True(output);

        return result;
    }
}
