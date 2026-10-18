using System.Net;
using System.Net.Http.Json;
using Common.Testing.Assert;

namespace Common.Testing.FluentTesting;

/// <summary>Assertions on the HTTP response of an API test. They fail with a message that names the expected and the actual value.</summary>
public static class AssertHttpFluentExtensions
{
    public static async Task<T> AssertStatusCode<T>(this Task<T> resultTask, HttpStatusCode expected)
        where T : ITestOutput<HttpResponseMessage>
    {
        var result = await resultTask;

        Xunit.Assert.Equal(expected, result.Output.StatusCode);
        return result;
    }

    /// <summary>The body is JSON that equals the expected value.</summary>
    public static async Task<T> AssertJson<T, TBody>(this Task<T> resultTask, TBody expected)
        where T : ITestOutput<HttpResponseMessage>
    {
        var result = await resultTask;

        AssertExtensions.DeepEqual(expected, await result.Output.Content.ReadFromJsonAsync<TBody>());
        return result;
    }

    /// <summary>Runs your assertions on the text of the response body.</summary>
    public static async Task<T> AssertResponseContent<T>(this Task<T> resultTask, Action<string> assert)
        where T : ITestOutput<HttpResponseMessage>
    {
        var result = await resultTask;

        assert(await result.Output.Content.ReadAsStringAsync());
        return result;
    }

    /// <summary>Runs your assertions on the HTTP response.</summary>
    public static async Task<T> AssertResponse<T>(this Task<T> resultTask, Action<HttpResponseMessage> assert)
        where T : ITestOutput<HttpResponseMessage>
    {
        var result = await resultTask;

        assert(result.Output);
        return result;
    }
}
