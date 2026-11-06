namespace PerfCompare.Server.Database;

public static class DatabaseInitializer
{
    public static void Initialize(SqliteConnectionFactory factory)
    {
        var sql = ReadSchema();

        using var connection = factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string ReadSchema()
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        using var stream = assembly.GetManifestResourceStream("schema.sql")
            ?? throw new InvalidOperationException("schema.sql не знайдено у збірці");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
