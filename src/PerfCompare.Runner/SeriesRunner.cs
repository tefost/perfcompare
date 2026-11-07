using System.Globalization;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public static class SeriesRunner
{
    public static RunResult Run(
        Scenario scenario, ProcessExecutor executor, EnvironmentInfo environment, TextWriter? progress = null)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var measurements = new List<Measurement>(scenario.Repeats);

        for (var iteration = 1; iteration <= scenario.Repeats; iteration++)
        {
            var result = executor.RunOnce(scenario);
            measurements.Add(new Measurement(
                iteration, result.DurationMs, result.PeakMemMb, result.CpuTimeMs, result.ExitCode, result.Status));

            progress?.WriteLine(
                $"[{iteration}/{scenario.Repeats}] {result.Status} {result.DurationMs.ToString("F1", CultureInfo.InvariantCulture)} мс");
        }

        var info = new ScenarioInfo(scenario.Name, FormatCommand(scenario), scenario.Repeats, scenario.TimeoutSeconds);
        return new RunResult(info, environment, startedAt, measurements);
    }

    private static string FormatCommand(Scenario scenario)
    {
        return scenario.Args.Count == 0
            ? scenario.Command
            : $"{scenario.Command} {string.Join(' ', scenario.Args)}";
    }
}
