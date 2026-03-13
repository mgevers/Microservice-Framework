using Common.Testing.Logging;
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
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<IServiceCollection>? configureServices = null)
    {
        return new ApiTestSetup<TFactory, TEntry>(
            factory,
            databaseState ?? DatabaseState.Empty,
            isReadOnlyDatabase,
            authToken: null,
            apiKey: null,
            loggingConfiguration,
            configureServices);
    }

    public static ApiTestSetup<TFactory, TEntry> ArrangeWithAuthToken(
        TFactory factory,
        DatabaseState? databaseState = null,
        string? authToken = null,
        bool isReadOnlyDatabase = false,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<IServiceCollection>? configureServices = null)
    {
        return new ApiTestSetup<TFactory, TEntry>(
            factory,
            databaseState ?? DatabaseState.Empty,
            isReadOnlyDatabase,
            authToken,
            apiKey: null,
            loggingConfiguration,
            configureServices);
    }

    public static ApiTestSetup<TFactory, TEntry> ArrangeWithApiKey(
        TFactory factory,
        DatabaseState? databaseState = null,
        string? apiKey = null,
        bool isReadOnlyDatabase = false,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<IServiceCollection>? configureServices = null)
    {
        return new ApiTestSetup<TFactory, TEntry>(
            factory,
            databaseState ?? DatabaseState.Empty,
            isReadOnlyDatabase,
            authToken: null,
            apiKey,
            loggingConfiguration,
            configureServices);
    }

    private ApiTestSetup(
        TFactory factory,
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        string? authToken,
        string? apiKey,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<IServiceCollection>? configureServices = null)
    {
        Factory = factory;
        IsReadOnlyDatabase = isReadOnlyDatabase;
        DatabaseState = databaseState;
        AuthToken = authToken;
        ApiKey = apiKey;
        LoggingConfiguration = loggingConfiguration;
        ConfigureServices = configureServices;
    }

    public TFactory Factory { get; }
    public bool IsReadOnlyDatabase { get; }
    public DatabaseState DatabaseState { get; }
    public string? AuthToken { get; }
    public Action<IServiceCollection>? ConfigureServices { get; }
    public string? ApiKey { get; }
    public FakeLoggingConfiguration? LoggingConfiguration { get; }
}
