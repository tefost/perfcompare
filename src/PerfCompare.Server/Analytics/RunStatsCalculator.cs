using PerfCompare.Contracts;

namespace PerfCompare.Server.Analytics;

public static class RunStatsCalculator
{
    public static RunStats Compute(RunResult run)
    {
        var measurements = run.Measurements;
        var passed = measurements.Where(m => m.Status == MeasurementStatus.Passed).ToList();
        var failed = measurements.Count(m => m.Status == MeasurementStatus.Failed);
        var timeouts = measurements.Count(m => m.Status == MeasurementStatus.Timeout);
        var total = measurements.Count;
        var successRate = total == 0 ? 0.0 : Math.Round((double)passed.Count / total, 4);

        var durations = passed.Select(m => m.DurationMs).ToList();
        DurationStats? duration = durations.Count == 0 ? null : BuildDuration(durations);

        return new RunStats(
            total,
            passed.Count,
            failed,
            timeouts,
            successRate,
            duration,
            MeanOrNull(passed.Select(m => m.PeakMemMb)),
            MeanOrNull(passed.Select(m => m.CpuTimeMs)),
            StabilityIndex(duration, successRate));
    }

    private static DurationStats BuildDuration(IReadOnlyList<double> values)
    {
        return new DurationStats(
            values.Min(),
            values.Max(),
            Statistics.Mean(values),
            Statistics.Median(values),
            Statistics.Percentile(values, 95),
            Statistics.StandardDeviation(values),
            Statistics.CoefficientOfVariationPercent(values));
    }

    private static double StabilityIndex(DurationStats? duration, double successRate)
    {
        if (duration is null)
        {
            return 0;
        }

        var evenness = Math.Clamp(100.0 - duration.CvPercent, 0.0, 100.0);
        return Math.Round(evenness * successRate, 1);
    }

    private static double? MeanOrNull(IEnumerable<double?> values)
    {
        var present = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return present.Count == 0 ? null : Statistics.Mean(present);
    }
}
