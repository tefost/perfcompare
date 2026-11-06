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

    public long SaveRun(RunResult run)
    {
        using var connection = _factory.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var environmentId = FindOrInsertEnvironment(connection, transaction, run.Environment);
            var scenarioId = FindOrInsertScenario(connection, transaction, run.Scenario.Name);
            var runId = InsertRun(connection, transaction, scenarioId, environmentId, run);

            foreach (var measurement in run.Measurements)
            {
                InsertMeasurement(connection, transaction, runId, measurement);
            }

            transaction.Commit();
            return runId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<RunSummary> ListRuns()
    {
        const string sql = @"SELECT r.id, s.name, e.id, e.os, r.started_at,
                (SELECT COUNT(*) FROM measurements m WHERE m.run_id = r.id)
            FROM runs r
            JOIN scenarios s ON s.id = r.scenario_id
            JOIN environments e ON e.id = r.environment_id
            ORDER BY r.id DESC;";

        using var connection = _factory.Open();
        using var command = CreateCommand(connection, null, sql);
        using var reader = command.ExecuteReader();

        var result = new List<RunSummary>();
        while (reader.Read())
        {
            result.Add(new RunSummary(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt64(2),
                reader.GetString(3),
                ParseDate(reader.GetString(4)),
                reader.GetInt32(5)));
        }

        return result;
    }

    public RunResult? GetRun(long id)
    {
        const string sql = @"SELECT s.name, r.command, r.repeats, r.timeout_seconds,
                e.os, e.os_version, e.runtime, e.cores, e.ram_mb, r.started_at
            FROM runs r
            JOIN scenarios s ON s.id = r.scenario_id
            JOIN environments e ON e.id = r.environment_id
            WHERE r.id = $id;";

        using var connection = _factory.Open();

        ScenarioInfo scenario;
        EnvironmentInfo environment;
        DateTimeOffset startedAt;
        using (var command = CreateCommand(connection, null, sql))
        {
            Add(command, "$id", id);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            scenario = new ScenarioInfo(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3));
            environment = new EnvironmentInfo(
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetInt32(7), reader.GetInt64(8));
            startedAt = ParseDate(reader.GetString(9));
        }

        return new RunResult(scenario, environment, startedAt, ReadMeasurements(connection, id));
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

    private static long InsertRun(SqliteConnection connection, SqliteTransaction transaction, long scenarioId, long environmentId, RunResult run)
    {
        const string sql = "INSERT INTO runs (scenario_id, environment_id, started_at, command, repeats, timeout_seconds) VALUES ($scenarioId, $environmentId, $startedAt, $command, $repeats, $timeoutSeconds);";
        using var command = CreateCommand(connection, transaction, sql);
        Add(command, "$scenarioId", scenarioId);
        Add(command, "$environmentId", environmentId);
        Add(command, "$startedAt", run.StartedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        Add(command, "$command", run.Scenario.Command);
        Add(command, "$repeats", run.Scenario.Repeats);
        Add(command, "$timeoutSeconds", run.Scenario.TimeoutSeconds);
        command.ExecuteNonQuery();
        return LastInsertId(connection, transaction);
    }

    private static void InsertMeasurement(SqliteConnection connection, SqliteTransaction transaction, long runId, Measurement measurement)
    {
        const string sql = "INSERT INTO measurements (run_id, iteration, duration_ms, peak_mem_mb, cpu_time_ms, exit_code, status) VALUES ($runId, $iteration, $durationMs, $peakMemMb, $cpuTimeMs, $exitCode, $status);";
        using var command = CreateCommand(connection, transaction, sql);
        Add(command, "$runId", runId);
        Add(command, "$iteration", measurement.Iteration);
        Add(command, "$durationMs", measurement.DurationMs);
        Add(command, "$peakMemMb", measurement.PeakMemMb);
        Add(command, "$cpuTimeMs", measurement.CpuTimeMs);
        Add(command, "$exitCode", measurement.ExitCode);
        Add(command, "$status", measurement.Status.ToString());
        command.ExecuteNonQuery();
    }

    private static List<Measurement> ReadMeasurements(SqliteConnection connection, long runId)
    {
        const string sql = "SELECT iteration, duration_ms, peak_mem_mb, cpu_time_ms, exit_code, status FROM measurements WHERE run_id = $runId ORDER BY iteration;";
        using var command = CreateCommand(connection, null, sql);
        Add(command, "$runId", runId);
        using var reader = command.ExecuteReader();

        var result = new List<Measurement>();
        while (reader.Read())
        {
            result.Add(new Measurement(
                reader.GetInt32(0),
                reader.GetDouble(1),
                reader.IsDBNull(2) ? null : reader.GetDouble(2),
                reader.IsDBNull(3) ? null : reader.GetDouble(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Enum.Parse<MeasurementStatus>(reader.GetString(5))));
        }

        return result;
    }

    private static DateTimeOffset ParseDate(string value)
    {
        return DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
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
