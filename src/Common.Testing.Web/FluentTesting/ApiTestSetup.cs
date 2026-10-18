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
        string? jwt = null,
        Action<IServiceCollection>? configureServices = null,
        string? apiKey = null)
    {
        Factory = factory;
        DatabaseState = databaseState;
        IsReadOnlyDatabase = isReadOnlyDatabase;
        Jwt = jwt;
        ConfigureServices = configureServices;
        ApiKey = apiKey;
    }

    public TFactory Factory { get; }
    public bool IsReadOnlyDatabase { get; }
    public DatabaseState DatabaseState { get; }
    public string? Jwt { get; }
    public Action<IServiceCollection>? ConfigureServices { get; }

    public string? ApiKey { get; }
}
