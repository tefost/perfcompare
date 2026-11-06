using System.Globalization;
using Microsoft.Data.Sqlite;
using PerfCompare.Contracts;

namespace PerfCompare.Server.Database;

public sealed class RunRepository
{
    private readonly SqliteConnectionFactory _factory;

    public RunRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public long GetOrCreateEnvironment(EnvironmentInfo environment)
    {
        using var connection = _factory.Open();
        return FindOrInsertEnvironment(connection, null, environment);
    }

    public long GetOrCreateScenario(string name)
    {
        using var connection = _factory.Open();
        return FindOrInsertScenario(connection, null, name);
    }

    private static long FindOrInsertEnvironment(SqliteConnection connection, SqliteTransaction? transaction, EnvironmentInfo environment)
    {
        const string select = "SELECT id FROM environments WHERE os = $os AND os_version = $osVersion AND runtime = $runtime AND cores = $cores AND ram_mb = $ramMb;";
        using (var find = CreateCommand(connection, transaction, select))
        {
            AddEnvironmentParameters(find, environment);
            if (find.ExecuteScalar() is long existing)
            {
                return existing;
            }
        }

        const string insert = "INSERT INTO environments (os, os_version, runtime, cores, ram_mb) VALUES ($os, $osVersion, $runtime, $cores, $ramMb);";
        using var create = CreateCommand(connection, transaction, insert);
        AddEnvironmentParameters(create, environment);
        create.ExecuteNonQuery();
        return LastInsertId(connection, transaction);
    }

    private static long FindOrInsertScenario(SqliteConnection connection, SqliteTransaction? transaction, string name)
    {
        using (var find = CreateCommand(connection, transaction, "SELECT id FROM scenarios WHERE name = $name;"))
        {
            Add(find, "$name", name);
            if (find.ExecuteScalar() is long existing)
            {
                return existing;
            }
        }

        using var create = CreateCommand(connection, transaction, "INSERT INTO scenarios (name) VALUES ($name);");
        Add(create, "$name", name);
        create.ExecuteNonQuery();
        return LastInsertId(connection, transaction);
    }

    private static void AddEnvironmentParameters(SqliteCommand command, EnvironmentInfo environment)
    {
        Add(command, "$os", environment.Os);
        Add(command, "$osVersion", environment.OsVersion);
        Add(command, "$runtime", environment.Runtime);
        Add(command, "$cores", environment.Cores);
        Add(command, "$ramMb", environment.RamMb);
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private static void Add(SqliteCommand command, string name, object? value)
    {
        command.Parameters.Add(new SqliteParameter(name, value ?? DBNull.Value));
    }

    private static long LastInsertId(SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = CreateCommand(connection, transaction, "SELECT last_insert_rowid();");
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
