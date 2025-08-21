using Common.Infrastructure.Auth.Token;
using Common.Testing.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Testing.Integration.FluentTesting;

public class ApiTestSetup<TFactory, TEntry>
    where TFactory : WebApplicationFactory<TEntry>
    where TEntry : class
{
    public ApiTestSetup(
        TFactory factory,
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        OAuth2Token? oAuth2Token,
        Action<IServiceCollection>? configureServices = null)
    {
        Factory = factory;
        DatabaseState = databaseState;
        IsReadOnlyDatabase = isReadOnlyDatabase;
        OAuth2Token = oAuth2Token;
        ConfigureServices = configureServices;
    }

    public ApiTestSetup(
        TFactory factory,
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        string? apiKey,
        Action<IServiceCollection>? configureServices = null)
    {
        Factory = factory;
        IsReadOnlyDatabase = isReadOnlyDatabase;
        DatabaseState = databaseState;
        ApiKey = apiKey;
        ConfigureServices = configureServices;
    }

    public TFactory Factory { get; }
    public bool IsReadOnlyDatabase { get; }
    public DatabaseState DatabaseState { get; }
    public OAuth2Token? OAuth2Token { get; }
    public Action<IServiceCollection>? ConfigureServices { get; }

    public string? ApiKey { get; }
}
