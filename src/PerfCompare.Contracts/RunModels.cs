namespace PerfCompare.Contracts;

public sealed record ScenarioInfo(string Name, string Command, int Repeats, int TimeoutSeconds);

public sealed record EnvironmentInfo(string Os, string OsVersion, string Runtime, int Cores, long RamMb);

public sealed record Measurement(
    int Iteration,
    double DurationMs,
    double? PeakMemMb,
    double? CpuTimeMs,
    int? ExitCode,
    MeasurementStatus Status);

public sealed record RunResult(
    ScenarioInfo Scenario,
    EnvironmentInfo Environment,
    DateTimeOffset StartedAt,
    IReadOnlyList<Measurement> Measurements);

public sealed record RunCreated(long RunId);
