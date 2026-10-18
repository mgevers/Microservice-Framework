using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Common.Testing.Integration.Containers;

/// <summary>
/// Percona Postgres with pg_tde, set up by the same script that docker compose uses (<c>01-pg-tde.sh</c>).
/// Tables created by the migrations are encrypted, as they are in the real services.
/// The script makes the roles of every service: <c>gamestats_app</c>, <c>gamestats_api</c>, <c>accounts_app</c> and <c>accounts_api</c>.
/// </summary>
public class TdePostgresContainer : IAsyncLifetime
{
    public const string AppPassword = "app-test-password";
    public const string ApiPassword = "api-test-password";

    private const uint ScriptMode = 0b111_101_101; // rwxr-xr-x

    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("percona/percona-distribution-postgresql:17")
        .WithCommand("-c", "shared_preload_libraries=pg_tde")
        .WithEnvironment("GAMESTATS_APP_PASSWORD", AppPassword)
        .WithEnvironment("GAMESTATS_API_PASSWORD", ApiPassword)
        .WithEnvironment("ACCOUNTS_APP_PASSWORD", AppPassword)
        .WithEnvironment("ACCOUNTS_API_PASSWORD", ApiPassword)
        .WithResourceMapping(
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "01-pg-tde.sh")),
            "/docker-entrypoint-initdb.d/01-pg-tde.sh",
            ScriptMode)
        .Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.StopAsync();

    /// <summary>A connection string to a database of the container, for a role of the service.</summary>
    public string ConnectionString(string database, string user, string password) =>
        new NpgsqlConnectionStringBuilder(Container.GetConnectionString())
        {
            Database = database,
            Username = user,
            Password = password
        }.ConnectionString;

    /// <summary>A name for a new database, so that each test has its own.</summary>
    public static string NewDatabaseName(string service) => $"{service}_{Guid.NewGuid():N}";

    /// <summary>Runs the statement and returns the error of Postgres, or null if it worked.</summary>
    public static async Task<PostgresException?> TryExecute(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        try
        {
            await new NpgsqlCommand(sql, connection).ExecuteNonQueryAsync();
            return null;
        }
        catch (PostgresException ex)
        {
            return ex;
        }
    }
}
