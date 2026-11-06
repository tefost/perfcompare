using Microsoft.Data.Sqlite;

namespace PerfCompare.Server.Tests;

public class SchemaTests
{
    [Fact]
    public void Connection_HasForeignKeysEnabled()
    {
        using var db = new TestDb();
        using var connection = db.Factory.Open();

        Assert.Equal(1L, TestDb.Scalar(connection, "PRAGMA foreign_keys;"));
    }

    [Fact]
    public void Run_WithUnknownScenario_IsRejected()
    {
        using var db = new TestDb();
        using var connection = db.Factory.Open();
        TestDb.Exec(connection, "INSERT INTO environments (os, os_version, runtime, cores, ram_mb) VALUES ('Linux', '6', '.NET 9', 4, 8192);");

        Assert.Throws<SqliteException>(() => TestDb.Exec(connection,
            "INSERT INTO runs (scenario_id, environment_id, started_at, command, repeats, timeout_seconds) VALUES (999, 1, '2026-11-07T10:00:00Z', 'x', 1, 30);"));
    }

    [Fact]
    public void Measurement_WithUnknownStatus_IsRejected()
    {
        using var db = new TestDb();
        using var connection = db.Factory.Open();
        TestDb.Exec(connection, TestDb.SeedSql);

        Assert.Throws<SqliteException>(() => TestDb.Exec(connection,
            "INSERT INTO measurements (run_id, iteration, duration_ms, status) VALUES (1, 1, 10, 'Weird');"));
    }

    [Fact]
    public void Measurement_WithNegativeDuration_IsRejected()
    {
        using var db = new TestDb();
        using var connection = db.Factory.Open();
        TestDb.Exec(connection, TestDb.SeedSql);

        Assert.Throws<SqliteException>(() => TestDb.Exec(connection,
            "INSERT INTO measurements (run_id, iteration, duration_ms, status) VALUES (1, 1, -5, 'Passed');"));
    }

    [Fact]
    public void Run_WithZeroRepeats_IsRejected()
    {
        using var db = new TestDb();
        using var connection = db.Factory.Open();
        TestDb.Exec(connection, TestDb.SeedSql);

        Assert.Throws<SqliteException>(() => TestDb.Exec(connection,
            "INSERT INTO runs (scenario_id, environment_id, started_at, command, repeats, timeout_seconds) VALUES (1, 1, '2026-11-07T10:00:00Z', 'x', 0, 30);"));
    }

    [Fact]
    public void Environment_Duplicate_IsRejected()
    {
        using var db = new TestDb();
        using var connection = db.Factory.Open();
        TestDb.Exec(connection, TestDb.SeedSql);

        Assert.Throws<SqliteException>(() => TestDb.Exec(connection,
            "INSERT INTO environments (os, os_version, runtime, cores, ram_mb) VALUES ('Windows', '10', '.NET 9', 8, 16384);"));
    }

    [Fact]
    public void DeletingRun_RemovesItsMeasurements()
    {
        using var db = new TestDb();
        using var connection = db.Factory.Open();
        TestDb.Exec(connection, TestDb.SeedSql);
        TestDb.Exec(connection, "INSERT INTO measurements (run_id, iteration, duration_ms, status) VALUES (1, 1, 10, 'Passed');");

        TestDb.Exec(connection, "DELETE FROM runs WHERE id = 1;");

        Assert.Equal(0L, TestDb.Scalar(connection, "SELECT COUNT(*) FROM measurements;"));
    }

    [Fact]
    public void LookupIndexes_Exist()
    {
        using var db = new TestDb();
        using var connection = db.Factory.Open();

        var count = TestDb.Scalar(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name IN ('idx_runs_scenario', 'idx_runs_environment', 'idx_measurements_run');");

        Assert.Equal(3L, count);
    }
}
