using PerfCompare.Contracts;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class SeriesRunnerResilienceTests
{
    private static readonly EnvironmentInfo Environment = new("Linux", "6", ".NET 9", 4, 8192);

    private static Scenario MakeScenario(int repeats, int warmup = 0) => new()
    {
        Name = "resilience",
        Command = "app",
        Repeats = repeats,
        TimeoutSeconds = 20,
        Warmup = warmup
    };

    private static ProcessResult Passed(double duration) => new(duration, 0, MeasurementStatus.Passed);

    [Fact]
    public void Run_WithWarmup_ExecutesWarmupButRecordsOnlyRepeats()
    {
        var calls = 0;

        var run = SeriesRunner.Run(MakeScenario(3, warmup: 2), _ =>
        {
            calls++;
            return Passed(10);
        }, Environment);

        Assert.Equal(5, calls);
        Assert.Equal(3, run.Measurements.Count);
        Assert.Equal(new[] { 1, 2, 3 }, run.Measurements.Select(m => m.Iteration));
    }

    [Fact]
    public void Run_WarmupResults_DoNotAppearInMeasurements()
    {
        var calls = 0;

        var run = SeriesRunner.Run(MakeScenario(2, warmup: 2), _ => Passed(++calls <= 2 ? 1000 : 10), Environment);

        Assert.All(run.Measurements, m => Assert.Equal(10.0, m.DurationMs, 6));
    }

    [Fact]
    public void Run_WithWarmup_WritesWarmupProgressLines()
    {
        var progress = new StringWriter();

        SeriesRunner.Run(MakeScenario(1, warmup: 2), _ => Passed(5), Environment, progress);

        var text = progress.ToString();
        Assert.Contains("[прогрів 1/2]", text);
        Assert.Contains("[прогрів 2/2]", text);
        Assert.Contains("[1/1]", text);
    }

    [Fact]
    public void Run_NoWarmup_WritesNoWarmupLines()
    {
        var progress = new StringWriter();

        SeriesRunner.Run(MakeScenario(2), _ => Passed(5), Environment, progress);

        Assert.DoesNotContain("прогрів", progress.ToString());
    }

    [Fact]
    public void Run_IterationThrows_BecomesFailedAndSeriesContinues()
    {
        var calls = 0;

        var run = SeriesRunner.Run(MakeScenario(3), _ =>
        {
            calls++;
            return calls == 2 ? throw new InvalidOperationException("збій") : Passed(10);
        }, Environment);

        Assert.Equal(3, run.Measurements.Count);
        Assert.Equal(MeasurementStatus.Passed, run.Measurements[0].Status);
        Assert.Equal(MeasurementStatus.Failed, run.Measurements[1].Status);
        Assert.Equal(MeasurementStatus.Passed, run.Measurements[2].Status);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(NotSupportedException))]
    public void Run_KnownExceptionTypes_AreConvertedToFailed(Type exceptionType)
    {
        var run = SeriesRunner.Run(
            MakeScenario(1),
            _ => throw (Exception)Activator.CreateInstance(exceptionType)!,
            Environment);

        Assert.Equal(MeasurementStatus.Failed, run.Measurements[0].Status);
    }

    [Fact]
    public void Run_ProgrammingError_IsNotSwallowed()
    {
        Assert.Throws<ArgumentException>(() =>
            SeriesRunner.Run(MakeScenario(1), _ => throw new ArgumentException("помилка коду"), Environment));
    }

    [Fact]
    public void Run_WarmupThrows_DoesNotStopMeasuredSeries()
    {
        var calls = 0;

        var run = SeriesRunner.Run(MakeScenario(2, warmup: 1), _ =>
        {
            calls++;
            return calls == 1 ? throw new IOException("прогрів") : Passed(10);
        }, Environment);

        Assert.Equal(2, run.Measurements.Count);
        Assert.All(run.Measurements, m => Assert.Equal(MeasurementStatus.Passed, m.Status));
    }

    [Fact]
    public void Run_CancelledAfterTwoIterations_ReturnsPartialSeries()
    {
        using var source = new CancellationTokenSource();
        var calls = 0;

        var run = SeriesRunner.Run(MakeScenario(5), _ =>
        {
            if (++calls == 2)
            {
                source.Cancel();
            }

            return Passed(10);
        }, Environment, null, source.Token);

        Assert.Equal(2, run.Measurements.Count);
        Assert.Equal(5, run.Scenario.Repeats);
    }

    [Fact]
    public void Run_CancelledBeforeStart_ReturnsNoMeasurements()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var calls = 0;

        var run = SeriesRunner.Run(MakeScenario(3, warmup: 2), _ =>
        {
            calls++;
            return Passed(10);
        }, Environment, null, source.Token);

        Assert.Empty(run.Measurements);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Run_CancelledDuringWarmup_SkipsMeasuredSeries()
    {
        using var source = new CancellationTokenSource();
        var calls = 0;

        var run = SeriesRunner.Run(MakeScenario(3, warmup: 3), _ =>
        {
            if (++calls == 1)
            {
                source.Cancel();
            }

            return Passed(10);
        }, Environment, null, source.Token);

        Assert.Empty(run.Measurements);
        Assert.Equal(1, calls);
    }
}
