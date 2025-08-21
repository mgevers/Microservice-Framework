using Common.Infrastructure.Auth.ApiKey;
using Common.Testing.Integration.FluentTesting;
using Common.Testing.Logging;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;

namespace Common.Testing.Web.FluentTesting;

public static class WebApplicationFactoryTest
{
    public static async Task<ApiTestResult<TResponse>> Act<TFactory, TEntry, TResponse>(
        this ApiTestSetup<TFactory, TEntry> testSetup,
        Func<HttpClient, Task<TResponse>> func)
        where TFactory : WebApplicationFactory<TEntry>
        where TEntry : class
    {
        using (FakeLoggingDatabase.Initialize())
        using (FakeServiceBus.Initialize())
        using (FakeDatabase.SeedData(testSetup.DatabaseState, testSetup.IsReadOnlyDatabase))
        {
            testSetup.Factory.Server.PreserveExecutionContext = true;
            var httpClient = testSetup.Factory.CreateClient();

            if (testSetup.OAuth2Token != null)
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(testSetup.OAuth2Token.TokenType!, testSetup.OAuth2Token.AccessToken);
            }
            else if (testSetup.ApiKey != null)
            {
                httpClient.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.ApiKeyHeaderName, testSetup.ApiKey);
            }

            var response = await func(httpClient);

            return new ApiTestResult<TResponse>(
                FakeDatabase.DatabaseState,
                FakeServiceBus.ServiceBusState,
                FakeLoggingDatabase.Logs,
                response);
        }
    }
}
