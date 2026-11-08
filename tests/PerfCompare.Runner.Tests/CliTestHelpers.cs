using PerfCompare.Contracts;

namespace PerfCompare.Runner.Tests;

internal static class CliTestHelpers
{
    public static readonly string HelperDll = Path.Combine(AppContext.BaseDirectory, "PerfCompare.TestApp.dll");

    public static string TempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "perfcompare-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static string WriteScenario(string dir, string exitCode, int repeats = 2)
    {
        var path = Path.Combine(dir, "scenario.yaml");
        File.WriteAllText(path, $"""
            name: cli-test
            command: dotnet
            args: ['{HelperDll}', 'exit', '{exitCode}']
            repeats: {repeats}
            timeoutSeconds: 20
            """);
        return path;
    }

    public static RunResult SampleRun(string name = "baseline", int minute = 0) => new(
        new ScenarioInfo(name, "dotnet --version", 1, 30),
        new EnvironmentInfo("Linux", "6.8", ".NET 9.0.2", 4, 8192),
        new DateTimeOffset(2026, 11, 8, 10, minute, 0, TimeSpan.Zero),
        new[] { new Measurement(1, 50, 10, 40, 0, MeasurementStatus.Passed) });

    public static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
