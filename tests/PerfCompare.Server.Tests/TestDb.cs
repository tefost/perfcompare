using Microsoft.Data.Sqlite;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Tests;

internal sealed class TestDb : IDisposable
{
    public const string SeedSql = """
        INSERT INTO environments (os, os_version, runtime, cores, ram_mb) VALUES ('Windows', '10', '.NET 9', 8, 16384);
        INSERT INTO scenarios (name) VALUES ('baseline');
        INSERT INTO runs (scenario_id, environment_id, started_at, command, repeats, timeout_seconds)
            VALUES (1, 1, '2026-11-07T10:00:00Z', 'dotnet', 3, 30);
        """;

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"perfcompare-{Guid.NewGuid():N}.db");

    public TestDb()
    {
        Factory = new SqliteConnectionFactory($"Data Source={_path};Pooling=False");
        DatabaseInitializer.Initialize(Factory);
    }

    public SqliteConnectionFactory Factory { get; }

    public static void Exec(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public static long Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
