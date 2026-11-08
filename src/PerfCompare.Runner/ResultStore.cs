using System.Globalization;
using System.Text.Json;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public static class ResultStore
{
    public const string SentSuffix = ".sent";

    private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

    public static string Save(RunResult run, string directory)
    {
        var stamp = run.StartedAt.UtcDateTime.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        var path = Path.Combine(directory, $"{Sanitize(run.Scenario.Name)}-{stamp}.json");
        ResultFile.Write(path, run);
        return Path.GetFullPath(path);
    }

    public static IReadOnlyList<string>? ListPending(string path)
    {
        if (File.Exists(path))
        {
            return new[] { Path.GetFullPath(path) };
        }

        if (!Directory.Exists(path))
        {
            return null;
        }

        return Directory.GetFiles(path, "*.json")
            .Select(Path.GetFullPath)
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();
    }

    public static RunResult? TryLoad(string file)
    {
        try
        {
            var run = JsonSerializer.Deserialize<RunResult>(File.ReadAllText(file), ContractJson.Options);
            return run is { Scenario: not null, Environment: not null, Measurements: not null } ? run : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void MarkSent(string file)
    {
        File.Move(file, file + SentSuffix, overwrite: true);
    }

    private static string Sanitize(string name)
    {
        return string.Concat(name.Select(symbol => InvalidNameChars.Contains(symbol) ? '_' : symbol));
    }
}
