using Common.Infrastructure.Compliance;
using Common.Testing.Integration.Containers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Common.Infrastructure.Functional.Tests.Compliance;

public class PostgresTdeSchemaTests : IClassFixture<TdePostgresContainer>
{
    private const string RolePassword = "role-test-password";

    private readonly TdePostgresContainer postgres;

    public PostgresTdeSchemaTests(TdePostgresContainer postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task EnsureTablesAreEncrypted_PassesForEncryptedTablesOfTheSchema()
    {
        await using var database = await NewDatabase();
        await database.Execute("create schema x; create table x.secret (id int primary key) using tde_heap;");

        await using var dbContext = database.DbContext();
        await PostgresTde.EnsureTablesAreEncrypted(dbContext, "x", CancellationToken.None);
    }

    [Fact]
    public async Task EnsureTablesAreEncrypted_ThrowsNamingThePlainTableAndTheSchema()
    {
        await using var database = await NewDatabase();
        await database.Execute("""
            create schema x;
            create table x.secret (id int primary key) using tde_heap;
            create table x.leaky (id int) using heap;
            """);

        await using var dbContext = database.DbContext();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PostgresTde.EnsureTablesAreEncrypted(dbContext, "x", CancellationToken.None));

        Assert.Contains("leaky", exception.Message);
        Assert.Contains("'x'", exception.Message);
        Assert.DoesNotContain("secret", exception.Message);
    }

    [Fact]
    public async Task EnsureTablesAreEncrypted_LooksOnlyAtTheSchemaItIsGiven()
    {
        await using var database = await NewDatabase();
        await database.Execute("""
            create schema x;
            create schema y;
            create table x.secret (id int primary key) using tde_heap;
            create table y.leaky (id int) using heap;
            """);

        await using var dbContext = database.DbContext();
        await PostgresTde.EnsureTablesAreEncrypted(dbContext, "x", CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => PostgresTde.EnsureTablesAreEncrypted(dbContext, "y", CancellationToken.None));
    }

    [Fact]
    public async Task EnsureTablesAreEncrypted_WithoutASchemaStillChecksPublic()
    {
        await using var database = await NewDatabase();
        await database.Execute("""
            create schema x;
            create table x.leaky (id int) using heap;
            create table public.leaky_in_public (id int) using heap;
            """);

        await using var dbContext = database.DbContext();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PostgresTde.EnsureTablesAreEncrypted(dbContext, CancellationToken.None));

        Assert.Contains("leaky_in_public", exception.Message);
        Assert.DoesNotContain("x.leaky", exception.Message);
    }

    [Fact]
    public async Task GrantApiRole_OnASchemaLetsTheRoleSelectOnlyWhatItWasGranted()
    {
        await using var database = await NewDatabase();
        await using var role = await database.NewRole();
        await database.Execute("""
            create schema x;
            create table x.readable (id int primary key) using tde_heap;
            create table x.other (id int primary key) using tde_heap;
            create table x.added (id int primary key) using tde_heap;
            insert into x.readable values (1);
            insert into x.other values (1);
            """);

        await using var dbContext = database.DbContext();
        await PostgresTde.GrantApiRole(dbContext, "x", role.Name, ["readable"], ["added"], CancellationToken.None);

        Assert.Null(await TdePostgresContainer.TryExecute(role.ConnectionString, "select * from x.readable"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, (await TdePostgresContainer.TryExecute(role.ConnectionString, "select * from x.other"))?.SqlState);
        Assert.Null(await TdePostgresContainer.TryExecute(role.ConnectionString, "insert into x.added values (1)"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, (await TdePostgresContainer.TryExecute(role.ConnectionString, "update x.readable set id = 2"))?.SqlState);
    }

    [Fact]
    public async Task GrantApiRole_AcceptsAnEmptyInsertListAndAnEmptyReadList()
    {
        await using var database = await NewDatabase();
        await using var role = await database.NewRole();
        await database.Execute("""
            create schema x;
            create table x.readable (id int primary key) using tde_heap;
            create table x.added (id int primary key) using tde_heap;
            """);

        await using var dbContext = database.DbContext();

        await PostgresTde.GrantApiRole(dbContext, "x", role.Name, ["readable"], [], CancellationToken.None);
        Assert.Null(await TdePostgresContainer.TryExecute(role.ConnectionString, "select * from x.readable"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, (await TdePostgresContainer.TryExecute(role.ConnectionString, "insert into x.added values (1)"))?.SqlState);

        await PostgresTde.GrantApiRole(dbContext, "x", role.Name, [], ["added"], CancellationToken.None);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, (await TdePostgresContainer.TryExecute(role.ConnectionString, "select * from x.readable"))?.SqlState);
        Assert.Null(await TdePostgresContainer.TryExecute(role.ConnectionString, "insert into x.added values (1)"));
    }

    [Fact]
    public async Task GrantApiRole_WithoutASchemaGrantsOnPublic()
    {
        await using var database = await NewDatabase();
        await using var role = await database.NewRole();
        await database.Execute("""
            create table public.readable (id int primary key) using tde_heap;
            create table public.other (id int primary key) using tde_heap;
            """);

        await using var dbContext = database.DbContext();
        await PostgresTde.GrantApiRole(dbContext, role.Name, ["readable"], [], CancellationToken.None);

        Assert.Null(await TdePostgresContainer.TryExecute(role.ConnectionString, "select * from public.readable"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, (await TdePostgresContainer.TryExecute(role.ConnectionString, "select * from public.other"))?.SqlState);
    }

    private async Task<TestDatabase> NewDatabase()
    {
        var database = new TestDatabase(postgres, TdePostgresContainer.NewDatabaseName("schemas"));
        await database.Create();

        return database;
    }

    // A database of its own, so that tests do not see each other's schemas. Template1 has pg_tde, and tde_heap is the default.
    private sealed class TestDatabase(TdePostgresContainer postgres, string name) : IAsyncDisposable
    {
        private string AdminConnectionString(string database) => postgres.ConnectionString(database, "postgres", "postgres");

        public string ConnectionString => AdminConnectionString(name);

        public async Task Create() => await Run(AdminConnectionString("postgres"), $"create database \"{name}\"");

        public Task Execute(string sql) => Run(ConnectionString, sql);

        public DbContext DbContext() =>
            new(new DbContextOptionsBuilder().UseNpgsql(ConnectionString).Options);

        public async Task<TestRole> NewRole()
        {
            var roleName = $"api_{Guid.NewGuid():N}";
            await Run(AdminConnectionString("postgres"), $"create role \"{roleName}\" login password '{RolePassword}'");

            return new TestRole(this, roleName, postgres.ConnectionString(name, roleName, RolePassword));
        }

        public Task DropRole(string roleName) => Run(AdminConnectionString("postgres"), $"drop role if exists \"{roleName}\"");

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await Run(AdminConnectionString("postgres"), $"drop database if exists \"{name}\" with (force)");
        }

        private static async Task Run(string connectionString, string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await new NpgsqlCommand(sql, connection).ExecuteNonQueryAsync();
        }
    }

    private sealed class TestRole(TestDatabase database, string name, string connectionString) : IAsyncDisposable
    {
        public string Name => name;

        public string ConnectionString => connectionString;

        public async ValueTask DisposeAsync()
        {
            await database.Execute($"drop owned by \"{name}\"");
            await database.DropRole(name);
        }
    }
}
