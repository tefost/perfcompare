using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;

namespace PerfCompare.Server.Tests;

public class RunStatsCalculatorTests
{
    private static RunResult RunWith(params Measurement[] measurements)
    {
        return RunRepositoryTests.Sample() with { Measurements = measurements };
    }

    private static Measurement Passed(int iteration, double duration, double? memory = null, double? cpu = null)
    {
        return new Measurement(iteration, duration, memory, cpu, 0, MeasurementStatus.Passed);
    }

    [Fact]
    public void Compute_AllPassed_FillsDurationStats()
    {
        var stats = RunStatsCalculator.Compute(RunWith(
            Passed(1, 10), Passed(2, 20), Passed(3, 30), Passed(4, 40), Passed(5, 50)));

        Assert.Equal(5, stats.Total);
        Assert.Equal(5, stats.Passed);
        Assert.Equal(1.0, stats.SuccessRate, 4);
        Assert.NotNull(stats.Duration);
        Assert.Equal(10.0, stats.Duration!.Min, 6);
        Assert.Equal(50.0, stats.Duration.Max, 6);
        Assert.Equal(30.0, stats.Duration.Mean, 6);
        Assert.Equal(30.0, stats.Duration.Median, 6);
        Assert.Equal(50.0, stats.Duration.P95, 6);
        Assert.Equal(15.811388, stats.Duration.StdDev, 5);
        Assert.Equal(52.704628, stats.Duration.CvPercent, 5);
    }

    [Fact]
    public void Compute_StabilityIndex_IsEvennessTimesSuccessRate()
    {
        var stats = RunStatsCalculator.Compute(RunWith(
            Passed(1, 10), Passed(2, 20), Passed(3, 30), Passed(4, 40), Passed(5, 50)));

        Assert.Equal(47.3, stats.StabilityIndex, 1);
    }

    [Fact]
    public void Compute_IdenticalDurations_GiveStabilityHundred()
    {
        var stats = RunStatsCalculator.Compute(RunWith(Passed(1, 100), Passed(2, 100), Passed(3, 100)));

        Assert.Equal(100.0, stats.StabilityIndex, 1);
        Assert.Equal(0.0, stats.Duration!.StdDev, 6);
    }

    [Fact]
    public void Compute_FailedAndTimeout_AreCountedButNotInDurations()
    {
        var stats = RunStatsCalculator.Compute(RunWith(
            Passed(1, 100),
            Passed(2, 100),
            new Measurement(3, 5, null, null, 3, MeasurementStatus.Failed),
            new Measurement(4, 30000, null, null, null, MeasurementStatus.Timeout)));

        Assert.Equal(4, stats.Total);
        Assert.Equal(2, stats.Passed);
        Assert.Equal(1, stats.Failed);
        Assert.Equal(1, stats.Timeouts);
        Assert.Equal(0.5, stats.SuccessRate, 4);
        Assert.Equal(100.0, stats.Duration!.Max, 6);
        Assert.Equal(50.0, stats.StabilityIndex, 1);
    }

    [Fact]
    public void Compute_NothingPassed_HasNoDurationAndZeroStability()
    {
        var stats = RunStatsCalculator.Compute(RunWith(
            new Measurement(1, 5, null, null, 1, MeasurementStatus.Failed),
            new Measurement(2, 30000, null, null, null, MeasurementStatus.Timeout)));

        Assert.Null(stats.Duration);
        Assert.Equal(0.0, stats.SuccessRate, 4);
        Assert.Equal(0.0, stats.StabilityIndex, 1);
    }

    [Fact]
    public void Compute_MemoryAndCpu_AverageOnlyKnownValues()
    {
        var stats = RunStatsCalculator.Compute(RunWith(
            Passed(1, 10, 40, 100),
            Passed(2, 10, null, null),
            Passed(3, 10, 60, 300)));

        Assert.Equal(50.0, stats.MeanPeakMemMb!.Value, 6);
        Assert.Equal(200.0, stats.MeanCpuTimeMs!.Value, 6);
    }

    [Fact]
    public void Compute_NoMemoryReadings_GivesNull()
    {
        var stats = RunStatsCalculator.Compute(RunWith(Passed(1, 10), Passed(2, 12)));

        Assert.Null(stats.MeanPeakMemMb);
        Assert.Null(stats.MeanCpuTimeMs);
    }

    [Fact]
    public void Compute_VeryUnevenDurations_StabilityNeverBelowZero()
    {
        var stats = RunStatsCalculator.Compute(RunWith(Passed(1, 1), Passed(2, 1000)));

        Assert.True(stats.StabilityIndex >= 0);
    }
}
