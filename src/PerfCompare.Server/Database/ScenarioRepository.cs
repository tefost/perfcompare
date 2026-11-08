using Microsoft.Data.Sqlite;

namespace PerfCompare.Server.Database;

public sealed record ScenarioItem(long Id, string Name);

public sealed class ScenarioRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ScenarioRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<ScenarioItem> List()
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM scenarios ORDER BY id;";
        using SqliteDataReader reader = command.ExecuteReader();

        var items = new List<ScenarioItem>();
        while (reader.Read())
        {
            items.Add(new ScenarioItem(reader.GetInt64(0), reader.GetString(1)));
        }

        return items;
    }
}
