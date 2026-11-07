namespace PerfCompare.Runner;

public sealed class MetricsSampler
{
    private long? _peakBytes;
    private TimeSpan? _cpuTime;

    public double? PeakMemMb => _peakBytes is null ? null : _peakBytes.Value / (1024.0 * 1024.0);

    public double? CpuTimeMs => _cpuTime?.TotalMilliseconds;

    public void Sample(IMetricsProvider provider)
    {
        provider.Refresh();

        var peak = provider.ReadPeakWorkingSet();
        if (peak is long value && value > 0 && (_peakBytes is null || value > _peakBytes))
        {
            _peakBytes = value;
        }

        var cpu = provider.ReadCpuTime();
        if (cpu is not null)
        {
            _cpuTime = cpu;
        }
    }
}
