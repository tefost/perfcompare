using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public sealed record ProcessResult(
    double DurationMs,
    int? ExitCode,
    MeasurementStatus Status,
    string? Error = null);
