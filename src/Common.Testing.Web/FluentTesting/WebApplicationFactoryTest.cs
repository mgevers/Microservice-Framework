using Common.Infrastructure.Auth.ApiKey;
using Common.Testing.Integration.FluentTesting;
using Common.Testing.Logging;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
        using (var configuredFactory = CreateConfiguredFactory(testSetup))
        {
            var factory = configuredFactory ?? testSetup.Factory;
            factory.Server.PreserveExecutionContext = true;
            var httpClient = factory.CreateClient();

            if (testSetup.Jwt != null)
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", testSetup.Jwt);
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

    private static WebApplicationFactory<TEntry>? CreateConfiguredFactory<TFactory, TEntry>(
        ApiTestSetup<TFactory, TEntry> testSetup)
        where TFactory : WebApplicationFactory<TEntry>
        where TEntry : class
    {
        var configureServices = testSetup.ConfigureServices;

        return configureServices == null
            ? null
            : testSetup.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(configureServices));
    }
}
