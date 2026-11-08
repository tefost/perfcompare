using PerfCompare.Server.Analytics;

namespace PerfCompare.Server.Tests;

public class RegressionDetectorTests
{
    private static RunStats Stats(
        double? median = 100,
        double? p95 = 120,
        double? memory = 40,
        double? cpu = 90,
        double successRate = 1.0)
    {
        DurationStats? duration = median is null
            ? null
            : new DurationStats(median.Value, p95!.Value, median.Value, median.Value, p95.Value, 0, 0);
        return new RunStats(5, 5, 0, 0, successRate, duration, memory, cpu, 100);
    }

    private static MetricChange Find(IReadOnlyList<MetricChange> changes, string metric)
    {
        return changes.Single(change => change.Metric == metric);
    }

    [Fact]
    public void Detect_IdenticalRuns_HasNoRegression()
    {
        var changes = RegressionDetector.Detect(Stats(), Stats(), 10);

        Assert.Equal(5, changes.Count);
        Assert.All(changes, change => Assert.False(change.IsRegression));
        Assert.All(changes, change => Assert.Equal(0.0, change.ChangePercent, 6));
    }

    [Fact]
    public void Detect_MedianGrewBeyondThreshold_FlagsOnlyMedian()
    {
        var changes = RegressionDetector.Detect(Stats(), Stats(median: 120, p95: 120), 10);

        var median = Find(changes, RegressionMetrics.DurationMedian);
        Assert.True(median.IsRegression);
        Assert.Equal(20.0, median.ChangePercent, 6);
        Assert.Single(changes, change => change.IsRegression);
    }

    [Fact]
    public void Detect_GrowthExactlyAtThreshold_IsNotRegression()
    {
        var changes = RegressionDetector.Detect(Stats(), Stats(median: 110, p95: 120), 10);

        var median = Find(changes, RegressionMetrics.DurationMedian);
        Assert.Equal(10.0, median.ChangePercent, 6);
        Assert.False(median.IsRegression);
    }

    [Fact]
    public void Detect_FasterRun_IsNotRegression()
    {
        var changes = RegressionDetector.Detect(Stats(), Stats(median: 60, p95: 70), 10);

        var median = Find(changes, RegressionMetrics.DurationMedian);
        Assert.Equal(-40.0, median.ChangePercent, 6);
        Assert.False(median.IsRegression);
    }

    [Fact]
    public void Detect_MemoryGrowth_IsFlagged()
    {
        var changes = RegressionDetector.Detect(Stats(), Stats(memory: 60), 10);

        var memory = Find(changes, RegressionMetrics.PeakMemory);
        Assert.Equal(50.0, memory.ChangePercent, 6);
        Assert.True(memory.IsRegression);
    }

    [Fact]
    public void Detect_SuccessRateDrop_IsFlagged()
    {
        var changes = RegressionDetector.Detect(Stats(), Stats(successRate: 0.8), 10);

        var success = Find(changes, RegressionMetrics.SuccessRate);
        Assert.Equal(-20.0, success.ChangePercent, 6);
        Assert.True(success.IsRegression);
    }

    [Fact]
    public void Detect_SuccessRateGrowth_IsNotFlagged()
    {
        var changes = RegressionDetector.Detect(Stats(successRate: 0.5), Stats(successRate: 1.0), 10);

        Assert.False(Find(changes, RegressionMetrics.SuccessRate).IsRegression);
    }

    [Fact]
    public void Detect_MetricMissingInBaseline_IsSkipped()
    {
        var changes = RegressionDetector.Detect(Stats(memory: null), Stats(), 10);

        Assert.DoesNotContain(changes, change => change.Metric == RegressionMetrics.PeakMemory);
        Assert.Equal(4, changes.Count);
    }

    [Fact]
    public void Detect_CurrentRunWithoutSuccessfulMeasurements_SkipsDurationMetrics()
    {
        var changes = RegressionDetector.Detect(Stats(), Stats(median: null, p95: null, memory: null, cpu: null, successRate: 0), 10);

        Assert.Single(changes);
        Assert.Equal(RegressionMetrics.SuccessRate, changes[0].Metric);
        Assert.True(changes[0].IsRegression);
    }

    [Fact]
    public void Detect_ZeroBaselineValue_IsSkipped()
    {
        var changes = RegressionDetector.Detect(Stats(cpu: 0), Stats(cpu: 50), 10);

        Assert.DoesNotContain(changes, change => change.Metric == RegressionMetrics.CpuTime);
    }

    [Fact]
    public void Detect_LargerThresholdAcceptsSameGrowth()
    {
        var strict = RegressionDetector.Detect(Stats(), Stats(median: 130, p95: 120), 10);
        var lenient = RegressionDetector.Detect(Stats(), Stats(median: 130, p95: 120), 50);

        Assert.True(Find(strict, RegressionMetrics.DurationMedian).IsRegression);
        Assert.False(Find(lenient, RegressionMetrics.DurationMedian).IsRegression);
    }
}
