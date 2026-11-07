using PerfCompare.Contracts;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class SeriesRunnerTests
{
    private static readonly string HelperDll =
        Path.Combine(AppContext.BaseDirectory, "PerfCompare.TestApp.dll");

    private static readonly EnvironmentInfo Environment = new("Linux", "6", ".NET 9", 4, 8192);

    private static Scenario Helper(int repeats, params string[] args) => new()
    {
        Name = "series",
        Command = "dotnet",
        Args = new[] { HelperDll }.Concat(args).ToList(),
        Repeats = repeats,
        TimeoutSeconds = 20
    };

    [Fact]
    public void Run_FiveRepeats_GivesFivePassedMeasurements()
    {
        var run = SeriesRunner.Run(Helper(5, "exit", "0"), new ProcessExecutor(), Environment);

        Assert.Equal(5, run.Measurements.Count);
        Assert.All(run.Measurements, m => Assert.Equal(MeasurementStatus.Passed, m.Status));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, run.Measurements.Select(m => m.Iteration));
    }

    [Fact]
    public void Run_FailingApp_SeriesContinuesWithFailedStatus()
    {
        var run = SeriesRunner.Run(Helper(3, "exit", "3"), new ProcessExecutor(), Environment);

        Assert.Equal(3, run.Measurements.Count);
        Assert.All(run.Measurements, m =>
        {
            Assert.Equal(MeasurementStatus.Failed, m.Status);
            Assert.Equal(3, m.ExitCode);
        });
    }

    [Fact]
    public void Run_FillsScenarioAndEnvironmentFromInputs()
    {
        var run = SeriesRunner.Run(Helper(1, "exit", "0"), new ProcessExecutor(), Environment);

        Assert.Equal("series", run.Scenario.Name);
        Assert.Equal(1, run.Scenario.Repeats);
        Assert.Equal(20, run.Scenario.TimeoutSeconds);
        Assert.StartsWith("dotnet ", run.Scenario.Command);
        Assert.Equal(Environment, run.Environment);
        Assert.Equal(TimeSpan.Zero, run.StartedAt.Offset);
    }

    [Fact]
    public void Run_WritesProgressLinePerIteration()
    {
        var progress = new StringWriter();

        SeriesRunner.Run(Helper(2, "exit", "0"), new ProcessExecutor(), Environment, progress);

        var lines = progress.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("[1/2]", lines[0]);
    }
}
