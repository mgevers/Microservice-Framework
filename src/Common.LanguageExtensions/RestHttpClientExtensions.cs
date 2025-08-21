using CSharpFunctionalExtensions;
using Newtonsoft.Json;
using System.Text;

namespace Common.LanguageExtensions;

public static class RestHttpClientExtensions
{
    public static async Task<Result<T>> GetAsync<T>(this HttpClient httpClient, string url, object? body = null, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(url, body, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        return response.IsSuccessStatusCode
            ? Result.Success(JsonConvert.DeserializeObject<T>(json))!
            : Result.Failure<T>(await GetErrorMessage(response));
    }

    public static Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, string url, object? body = null, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (body != null)
        {
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        }

        return SendAsync(httpClient, request, cancellationToken);
    }

    public static Task<HttpResponseMessage> PostAsync(this HttpClient httpClient, string url, object body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };

        return SendAsync(httpClient, request, cancellationToken);
    }

    public static Task<HttpResponseMessage> PutAsync(this HttpClient httpClient, string url, object body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };

        return SendAsync(httpClient, request, cancellationToken);
    }

    public static Task<HttpResponseMessage> DeleteAsync(this HttpClient httpClient, string url, CancellationToken cancellationToken = default)
    {
        return SendAsync(httpClient, new HttpRequestMessage(HttpMethod.Delete, url), cancellationToken);
    }

    public static Task<HttpResponseMessage> DeleteAsync(this HttpClient httpClient, string url, object body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };

        return SendAsync(httpClient, request, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        }
    }

    private static async Task<string> GetErrorMessage(HttpResponseMessage response)
    {
        var responseJson = await response.Content.ReadAsStringAsync();

        return string.IsNullOrEmpty(responseJson)
            ? $"http response was not a successful status code - {response.StatusCode}"
            : responseJson;
    }
}
