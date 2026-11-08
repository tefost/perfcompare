using Microsoft.Data.Sqlite;

namespace PerfCompare.Server.Database;

public sealed record RunKey(long ScenarioId, long EnvironmentId);

public sealed class RunKeyReader
{
    private readonly SqliteConnectionFactory _factory;

    public RunKeyReader(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public RunKey? Find(long runId)
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT scenario_id, environment_id FROM runs WHERE id = $id;";
        command.Parameters.Add(new SqliteParameter("$id", runId));
        using var reader = command.ExecuteReader();
        return reader.Read() ? new RunKey(reader.GetInt64(0), reader.GetInt64(1)) : null;
    }
}
