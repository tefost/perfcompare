namespace PerfCompare.Server.Analytics;

public static class RegressionStatus
{
    public const string Ok = "ok";
    public const string Regression = "regression";
    public const string NoBaseline = "no_baseline";
}

public static class RegressionMetrics
{
    public const string DurationMedian = "durationMedianMs";
    public const string DurationP95 = "durationP95Ms";
    public const string PeakMemory = "meanPeakMemMb";
    public const string CpuTime = "meanCpuTimeMs";
    public const string SuccessRate = "successRate";
}

public sealed record MetricChange(
    string Metric,
    double Baseline,
    double Current,
    double ChangePercent,
    bool IsRegression);

public sealed record RegressionReport(
    long RunId,
    long? BaselineRunId,
    string Status,
    double ThresholdPercent,
    IReadOnlyList<MetricChange> Metrics);
