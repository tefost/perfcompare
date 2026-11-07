using System.Text.Json;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public static class ResultFile
{
    private static readonly JsonSerializerOptions Options = new(ContractJson.Options)
    {
        WriteIndented = true
    };

    public static void Write(string path, RunResult run)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = fullPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(run, Options));
        File.Move(temp, fullPath, overwrite: true);
    }
}
