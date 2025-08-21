using Common.Testing.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Testing.Integration.FluentTesting;

public class ApiTestSetup<TFactory, TEntry>
    where TFactory : WebApplicationFactory<TEntry>
    where TEntry : class
{
    public static ApiTestSetup<TFactory, TEntry> ArrangeWithoutAuth(
        TFactory factory,
        DatabaseState? databaseState = null,
        bool isReadOnlyDatabase = false,
        Action<IServiceCollection>? configureServices = null)
    {
        return new ApiTestSetup<TFactory, TEntry>(
            factory,
            databaseState ?? DatabaseState.Empty,
            isReadOnlyDatabase,
            authToken: null,
            apiKey: null,
            configureServices);
    }

    public static ApiTestSetup<TFactory, TEntry> ArrangeWithAuthToken(
        TFactory factory,
        DatabaseState? databaseState = null,
        string? authToken = null,
        bool isReadOnlyDatabase = false,
        Action<IServiceCollection>? configureServices = null)
    {
        return new ApiTestSetup<TFactory, TEntry>(
            factory,
            databaseState ?? DatabaseState.Empty,
            isReadOnlyDatabase,
            authToken,
            apiKey: null,
            configureServices);
    }

    public static ApiTestSetup<TFactory, TEntry> ArrangeWithApiKey(
        TFactory factory,
        DatabaseState? databaseState = null,
        string? apiKey = null,
        bool isReadOnlyDatabase = false,
        Action<IServiceCollection>? configureServices = null)
    {
        return new ApiTestSetup<TFactory, TEntry>(
            factory,
            databaseState ?? DatabaseState.Empty,
            isReadOnlyDatabase,
            authToken: null,
            apiKey,
            configureServices);
    }

    private ApiTestSetup(
        TFactory factory,
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        string? authToken,
        string? apiKey,
        Action<IServiceCollection>? configureServices = null)
    {
        Factory = factory;
        IsReadOnlyDatabase = isReadOnlyDatabase;
        DatabaseState = databaseState;
        AuthToken = authToken;
        ApiKey = apiKey;
        ConfigureServices = configureServices;
    }

    public TFactory Factory { get; }
    public bool IsReadOnlyDatabase { get; }
    public DatabaseState DatabaseState { get; }
    public string? AuthToken { get; }
    public Action<IServiceCollection>? ConfigureServices { get; }
    public string? ApiKey { get; }
}
