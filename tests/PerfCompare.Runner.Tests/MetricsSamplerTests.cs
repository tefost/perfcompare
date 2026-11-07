using System.Diagnostics;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class MetricsSamplerTests
{
    private sealed class FakeProvider : IMetricsProvider
    {
        private readonly Queue<(long? Peak, TimeSpan? Cpu)> _readings;
        private (long? Peak, TimeSpan? Cpu) _current;

        public FakeProvider(params (long? Peak, TimeSpan? Cpu)[] readings)
        {
            _readings = new Queue<(long? Peak, TimeSpan? Cpu)>(readings);
        }

        public void Refresh()
        {
            if (_readings.Count > 0)
            {
                _current = _readings.Dequeue();
            }
        }

        public long? ReadPeakWorkingSet() => _current.Peak;

        public TimeSpan? ReadCpuTime() => _current.Cpu;
    }

    [Fact]
    public void Sample_KeepsMaximumPeakAndLastCpuTime()
    {
        var provider = new FakeProvider(
            (10L * 1024 * 1024, TimeSpan.FromMilliseconds(50)),
            (30L * 1024 * 1024, TimeSpan.FromMilliseconds(120)),
            (20L * 1024 * 1024, TimeSpan.FromMilliseconds(200)));
        var sampler = new MetricsSampler();

        sampler.Sample(provider);
        sampler.Sample(provider);
        sampler.Sample(provider);

        Assert.Equal(30.0, sampler.PeakMemMb);
        Assert.Equal(200.0, sampler.CpuTimeMs);
    }

    [Fact]
    public void Sample_UnavailableMetrics_StayNull()
    {
        var provider = new FakeProvider((null, null), (null, null));
        var sampler = new MetricsSampler();

        sampler.Sample(provider);
        sampler.Sample(provider);

        Assert.Null(sampler.PeakMemMb);
        Assert.Null(sampler.CpuTimeMs);
    }

    [Fact]
    public void Sample_LaterNullDoesNotEraseEarlierValue()
    {
        var provider = new FakeProvider(
            (5L * 1024 * 1024, TimeSpan.FromMilliseconds(10)),
            (null, null));
        var sampler = new MetricsSampler();

        sampler.Sample(provider);
        sampler.Sample(provider);

        Assert.Equal(5.0, sampler.PeakMemMb);
        Assert.Equal(10.0, sampler.CpuTimeMs);
    }

    [Fact]
    public void Sample_ZeroPeak_IsTreatedAsUnavailable()
    {
        var provider = new FakeProvider((0L, TimeSpan.FromMilliseconds(10)));
        var sampler = new MetricsSampler();

        sampler.Sample(provider);

        Assert.Null(sampler.PeakMemMb);
        Assert.Equal(10.0, sampler.CpuTimeMs);
    }

    [Fact]
    public void ProcessMetricsProvider_ProcessNotStarted_ReturnsNull()
    {
        using var process = new Process();
        var provider = new ProcessMetricsProvider(process);

        provider.Refresh();

        Assert.Null(provider.ReadPeakWorkingSet());
        Assert.Null(provider.ReadCpuTime());
    }
}
