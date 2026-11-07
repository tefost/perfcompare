namespace PerfCompare.Runner;

public interface IMetricsProvider
{
    void Refresh();

    long? ReadPeakWorkingSet();

    TimeSpan? ReadCpuTime();
}
