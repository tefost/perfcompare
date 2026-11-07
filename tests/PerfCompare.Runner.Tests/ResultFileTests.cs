using System.Text.Json;
using PerfCompare.Contracts;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class ResultFileTests
{
    private static RunResult Sample() => new(
        new ScenarioInfo("baseline", "dotnet --version", 2, 30),
        new EnvironmentInfo("Linux", "6.8", ".NET 9.0.2", 4, 8192),
        new DateTimeOffset(2026, 11, 7, 10, 15, 0, TimeSpan.Zero),
        new[]
        {
            new Measurement(1, 50.5, 12.5, 40, 0, MeasurementStatus.Passed),
            new Measurement(2, 30000, null, null, null, MeasurementStatus.Timeout)
        });

    private static string TempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "perfcompare-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void Write_ThenRead_RoundTripsContract()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "result.json");

            ResultFile.Write(path, Sample());
            var loaded = JsonSerializer.Deserialize<RunResult>(File.ReadAllText(path), ContractJson.Options);

            Assert.NotNull(loaded);
            Assert.Equal(Sample().Scenario, loaded!.Scenario);
            Assert.Equal(2, loaded.Measurements.Count);
            Assert.Null(loaded.Measurements[1].PeakMemMb);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_UsesCamelCaseAndEnumNames()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "result.json");

            ResultFile.Write(path, Sample());
            var text = File.ReadAllText(path);

            Assert.Contains("\"timeoutSeconds\"", text);
            Assert.Contains("\"status\": \"Timeout\"", text);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_CreatesDirectoryAndLeavesNoTempFile()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "nested", "deeper", "result.json");

            ResultFile.Write(path, Sample());

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_ExistingFile_IsReplaced()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "result.json");
            File.WriteAllText(path, "old content");

            ResultFile.Write(path, Sample());

            Assert.DoesNotContain("old content", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
