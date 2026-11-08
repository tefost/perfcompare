using System.Globalization;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public static class SeriesRunner
{
    public static RunResult Run(
        Scenario scenario,
        ProcessExecutor executor,
        EnvironmentInfo environment,
        TextWriter? progress = null,
        CancellationToken cancellation = default)
    {
        return Run(scenario, executor.RunOnce, environment, progress, cancellation);
    }

    public static RunResult Run(
        Scenario scenario,
        Func<Scenario, ProcessResult> runOnce,
        EnvironmentInfo environment,
        TextWriter? progress = null,
        CancellationToken cancellation = default)
    {
        RunWarmup(scenario, runOnce, progress, cancellation);

        var startedAt = DateTimeOffset.UtcNow;
        var measurements = new List<Measurement>(scenario.Repeats);

        for (var iteration = 1; iteration <= scenario.Repeats && !cancellation.IsCancellationRequested; iteration++)
        {
            var result = Execute(runOnce, scenario);
            measurements.Add(new Measurement(
                iteration, result.DurationMs, result.PeakMemMb, result.CpuTimeMs, result.ExitCode, result.Status));

            progress?.WriteLine($"[{iteration}/{scenario.Repeats}] {Describe(result)}");
        }

        var info = new ScenarioInfo(scenario.Name, FormatCommand(scenario), scenario.Repeats, scenario.TimeoutSeconds);
        return new RunResult(info, environment, startedAt, measurements);
    }

    private static void RunWarmup(
        Scenario scenario, Func<Scenario, ProcessResult> runOnce, TextWriter? progress, CancellationToken cancellation)
    {
        for (var index = 1; index <= scenario.Warmup && !cancellation.IsCancellationRequested; index++)
        {
            var result = Execute(runOnce, scenario);
            progress?.WriteLine($"[прогрів {index}/{scenario.Warmup}] {Describe(result)}");
        }
    }

    private static ProcessResult Execute(Func<Scenario, ProcessResult> runOnce, Scenario scenario)
    {
        try
        {
            return runOnce(scenario);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new ProcessResult(0, null, MeasurementStatus.Failed, ex.Message);
        }
    }

    private static string Describe(ProcessResult result)
    {
        return $"{result.Status} {result.DurationMs.ToString("F1", CultureInfo.InvariantCulture)} мс";
    }

    private static string FormatCommand(Scenario scenario)
    {
        return scenario.Args.Count == 0
            ? scenario.Command
            : $"{scenario.Command} {string.Join(' ', scenario.Args)}";
    }
}
