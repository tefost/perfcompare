using System.Globalization;
using Microsoft.Data.Sqlite;
using PerfCompare.Contracts;

namespace PerfCompare.Server.Database;

public sealed class BaselineRepository
{
    private readonly SqliteConnectionFactory _factory;

    public BaselineRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public long? SetBaseline(long runId)
    {
        using var connection = _factory.Open();
        using var transaction = connection.BeginTransaction();

        var key = FindRunKey(connection, transaction, runId);
        if (key is null)
        {
            return null;
        }

        var (scenarioId, environmentId) = key.Value;
        var createdAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var existing = FindBaselineId(connection, transaction, scenarioId, environmentId);

        long baselineId;
        if (existing is long id)
        {
            const string update = "UPDATE baselines SET run_id = $runId, created_at = $createdAt WHERE id = $id;";
            using var command = CreateCommand(connection, transaction, update);
            Add(command, "$runId", runId);
            Add(command, "$createdAt", createdAt);
            Add(command, "$id", id);
            command.ExecuteNonQuery();
            baselineId = id;
        }
        else
        {
            const string insert = "INSERT INTO baselines (scenario_id, environment_id, run_id, created_at) VALUES ($scenarioId, $environmentId, $runId, $createdAt);";
            using var command = CreateCommand(connection, transaction, insert);
            Add(command, "$scenarioId", scenarioId);
            Add(command, "$environmentId", environmentId);
            Add(command, "$runId", runId);
            Add(command, "$createdAt", createdAt);
            command.ExecuteNonQuery();
            baselineId = LastInsertId(connection, transaction);
        }

        transaction.Commit();
        return baselineId;
    }

    public long? GetBaselineRunId(long scenarioId, long environmentId)
    {
        using var connection = _factory.Open();
        const string sql = "SELECT run_id FROM baselines WHERE scenario_id = $scenarioId AND environment_id = $environmentId;";
        using var command = CreateCommand(connection, null, sql);
        Add(command, "$scenarioId", scenarioId);
        Add(command, "$environmentId", environmentId);
        return command.ExecuteScalar() is long runId ? runId : null;
    }

    public string? GetScenarioName(long scenarioId)
    {
        using var connection = _factory.Open();
        using var command = CreateCommand(connection, null, "SELECT name FROM scenarios WHERE id = $id;");
        Add(command, "$id", scenarioId);
        return command.ExecuteScalar() as string;
    }

    public IReadOnlyList<EnvironmentLatestRun> ListLatestRuns(long scenarioId)
    {
        const string sql = @"SELECT e.id, e.os, e.os_version, e.runtime, e.cores, e.ram_mb, r.id, r.started_at
            FROM environments e
            JOIN runs r ON r.environment_id = e.id
            WHERE r.scenario_id = $scenarioId
              AND r.id = (SELECT MAX(r2.id) FROM runs r2
                          WHERE r2.scenario_id = r.scenario_id AND r2.environment_id = e.id)
            ORDER BY e.id;";

        using var connection = _factory.Open();
        using var command = CreateCommand(connection, null, sql);
        Add(command, "$scenarioId", scenarioId);
        using var reader = command.ExecuteReader();

        var result = new List<EnvironmentLatestRun>();
        while (reader.Read())
        {
            var environment = new EnvironmentInfo(
                reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetInt64(5));
            result.Add(new EnvironmentLatestRun(reader.GetInt64(0), environment, reader.GetInt64(6), ParseDate(reader.GetString(7))));
        }

        return result;
    }

    private static (long ScenarioId, long EnvironmentId)? FindRunKey(
        SqliteConnection connection, SqliteTransaction transaction, long runId)
    {
        using var command = CreateCommand(connection, transaction, "SELECT scenario_id, environment_id FROM runs WHERE id = $id;");
        Add(command, "$id", runId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetInt64(0), reader.GetInt64(1)) : null;
    }

    private static long? FindBaselineId(
        SqliteConnection connection, SqliteTransaction transaction, long scenarioId, long environmentId)
    {
        const string sql = "SELECT id FROM baselines WHERE scenario_id = $scenarioId AND environment_id = $environmentId;";
        using var command = CreateCommand(connection, transaction, sql);
        Add(command, "$scenarioId", scenarioId);
        Add(command, "$environmentId", environmentId);
        return command.ExecuteScalar() is long id ? id : null;
    }

    private static DateTimeOffset ParseDate(string value)
    {
        return DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
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

    private static long LastInsertId(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = CreateCommand(connection, transaction, "SELECT last_insert_rowid();");
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
