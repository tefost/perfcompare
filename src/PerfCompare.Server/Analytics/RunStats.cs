namespace PerfCompare.Server.Analytics;

public sealed record DurationStats(
    double Min,
    double Max,
    double Mean,
    double Median,
    double P95,
    double StdDev,
    double CvPercent);

public sealed record RunStats(
    int Total,
    int Passed,
    int Failed,
    int Timeouts,
    double SuccessRate,
    DurationStats? Duration,
    double? MeanPeakMemMb,
    double? MeanCpuTimeMs,
    double StabilityIndex);
