namespace PerfCompare.Server.Analytics;

public static class RegressionDetector
{
    private sealed record Candidate(string Name, double? Baseline, double? Current, bool HigherIsWorse);

    public static IReadOnlyList<MetricChange> Detect(RunStats baseline, RunStats current, double thresholdPercent)
    {
        var candidates = new[]
        {
            new Candidate(RegressionMetrics.DurationMedian, baseline.Duration?.Median, current.Duration?.Median, true),
            new Candidate(RegressionMetrics.DurationP95, baseline.Duration?.P95, current.Duration?.P95, true),
            new Candidate(RegressionMetrics.PeakMemory, baseline.MeanPeakMemMb, current.MeanPeakMemMb, true),
            new Candidate(RegressionMetrics.CpuTime, baseline.MeanCpuTimeMs, current.MeanCpuTimeMs, true),
            new Candidate(RegressionMetrics.SuccessRate, baseline.SuccessRate, current.SuccessRate, false)
        };

        var changes = new List<MetricChange>();
        foreach (var candidate in candidates)
        {
            if (candidate.Baseline is not double before || candidate.Current is not double after || before <= 0)
            {
                continue;
            }

            var change = Math.Round((after - before) / before * 100.0, 2);
            var worse = candidate.HigherIsWorse ? change > thresholdPercent : change < -thresholdPercent;
            changes.Add(new MetricChange(candidate.Name, before, after, change, worse));
        }

        return changes;
    }
}
