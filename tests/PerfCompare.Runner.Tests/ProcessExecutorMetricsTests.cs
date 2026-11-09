using PerfCompare.Contracts;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class ProcessExecutorMetricsTests
{
    private static readonly string HelperDll =
        Path.Combine(AppContext.BaseDirectory, "PerfCompare.TestApp.dll");

    private sealed class ConstantProvider : IMetricsProvider
    {
        private readonly long? _peak;
        private readonly TimeSpan? _cpu;

        public ConstantProvider(long? peak, TimeSpan? cpu)
        {
            _peak = peak;
            _cpu = cpu;
        }

        public void Refresh()
        {
        }

        public long? ReadPeakWorkingSet() => _peak;

        public TimeSpan? ReadCpuTime() => _cpu;
    }

    private static Scenario Helper(params string[] args) => new()
    {
        Name = "metrics",
        Command = "dotnet",
        Args = new[] { HelperDll }.Concat(args).ToList(),
        Repeats = 1,
        TimeoutSeconds = 20
    };

    [Fact]
    public void RunOnce_UsesValuesFromMetricsProvider()
    {
        var executor = new ProcessExecutor(
            providerFactory: _ => new ConstantProvider(50L * 1024 * 1024, TimeSpan.FromMilliseconds(120)));

        var result = executor.RunOnce(Helper("exit", "0"));

        Assert.Equal(MeasurementStatus.Passed, result.Status);
        Assert.Equal(50.0, result.PeakMemMb);
        Assert.Equal(120.0, result.CpuTimeMs);
    }

    [Fact]
    public void RunOnce_UnavailableMetrics_GiveNullAndRunDoesNotFail()
    {
        var executor = new ProcessExecutor(providerFactory: _ => new ConstantProvider(null, null));

        var result = executor.RunOnce(Helper("exit", "0"));

        Assert.Equal(MeasurementStatus.Passed, result.Status);
        Assert.Null(result.PeakMemMb);
        Assert.Null(result.CpuTimeMs);
    }

    [Fact]
    public void RunOnce_RealProvider_DoesNotThrow()
    {
        var result = new ProcessExecutor().RunOnce(Helper("sleep", "0.3"));

        Assert.Equal(MeasurementStatus.Passed, result.Status);
        Assert.True(result.PeakMemMb is null or >= 0);
    }

    [Fact]
    public void RunOnce_Timeout_KeepsSampledMetrics()
    {
        var scenario = Helper("sleep", "30");
        scenario.TimeoutSeconds = 1;
        var executor = new ProcessExecutor(
            providerFactory: _ => new ConstantProvider(8L * 1024 * 1024, TimeSpan.FromMilliseconds(5)));

        var result = executor.RunOnce(scenario);

        Assert.Equal(MeasurementStatus.Timeout, result.Status);
        Assert.Equal(8.0, result.PeakMemMb);
    }
}
