using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Common.Infrastructure.Compliance;

public static class PostgresTde
{
    /// <summary>
    /// Checks that Postgres stores every table and index of the public schema encrypted with pg_tde.
    /// It throws if it does not, so a service never runs against plain tables by mistake.
    /// </summary>
    public static Task EnsureTablesAreEncrypted(DbContext dbContext, CancellationToken cancellationToken) =>
        EnsureTablesAreEncrypted(dbContext, "public", cancellationToken);

    /// <summary>
    /// Checks that Postgres stores every table and index of the schema encrypted with pg_tde.
    /// It throws if it does not, so a service never runs against plain tables by mistake.
    /// </summary>
    public static async Task EnsureTablesAreEncrypted(DbContext dbContext, string schema, CancellationToken cancellationToken)
    {
        List<string> plainRelations;
        try
        {
            // The schema goes in as a parameter of the query, never into its text.
            plainRelations = await dbContext.Database
                .SqlQuery<string>($"""
                    select c.relname as "Value"
                    from pg_class c
                    join pg_namespace n on n.oid = c.relnamespace
                    where n.nspname = {schema} and c.relkind in ('r', 'i') and not pg_tde_is_encrypted(c.oid::regclass)
                    """)
                .ToListAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedFunction)
        {
            throw new InvalidOperationException(
                "The pg_tde extension is not installed in this database, so the tables are not encrypted. " +
                "Run Postgres with pg_tde, as in GameStats/docker/postgres/01-pg-tde.sh.", ex);
        }

        if (plainRelations.Count > 0)
        {
            throw new InvalidOperationException(
                $"These tables or indexes of schema '{schema}' are not encrypted by pg_tde: {string.Join(", ", plainRelations)}. " +
                "Create the tables with the tde_heap access method, for example by setting default_table_access_method.");
        }
    }

    public static string QuoteIdentifier(string name) => new NpgsqlCommandBuilder().QuoteIdentifier(name);

    /// <summary>
    /// Gives the API role <c>SELECT</c> on the tables it reads and <c>INSERT</c> on the tables it adds to, and nothing else.
    /// It cannot change data, read the outbox, or change the schema. The table names come from the code of the service.
    /// </summary>
    public static Task GrantApiRole(
        DbContext dbContext,
        string role,
        IReadOnlyCollection<string> readTables,
        IReadOnlyCollection<string> insertTables,
        CancellationToken cancellationToken) =>
        GrantApiRole(dbContext, "public", role, readTables, insertTables, cancellationToken);

    /// <summary>
    /// The same as above, for the tables of <paramref name="schema"/>. A list with no tables grants nothing.
    /// </summary>
    public static async Task GrantApiRole(
        DbContext dbContext,
        string schema,
        string role,
        IReadOnlyCollection<string> readTables,
        IReadOnlyCollection<string> insertTables,
        CancellationToken cancellationToken)
    {
        var quotedSchema = QuoteIdentifier(schema);
        var quotedRole = QuoteIdentifier(role);
        string Tables(IEnumerable<string> names) => string.Join(", ", names.Select(name => $"{quotedSchema}.{QuoteIdentifier(name)}"));

        List<string> statements =
        [
            $"GRANT USAGE ON SCHEMA {quotedSchema} TO {quotedRole};",
            $"REVOKE ALL ON ALL TABLES IN SCHEMA {quotedSchema} FROM {quotedRole};"
        ];

        if (readTables.Count > 0)
        {
            statements.Add($"GRANT SELECT ON {Tables(readTables)} TO {quotedRole};");
        }

        if (insertTables.Count > 0)
        {
            statements.Add($"GRANT INSERT ON {Tables(insertTables)} TO {quotedRole};");
        }

        await dbContext.Database.ExecuteSqlRawAsync(string.Join(Environment.NewLine, statements), cancellationToken);
    }
}
