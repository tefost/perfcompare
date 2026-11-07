using System.ComponentModel;
using System.Diagnostics;

namespace PerfCompare.Runner;

public sealed class ProcessMetricsProvider : IMetricsProvider
{
    private readonly Process _process;

    public ProcessMetricsProvider(Process process)
    {
        _process = process;
    }

    public void Refresh()
    {
        _ = Read(() =>
        {
            _process.Refresh();
            return true;
        });
    }

    public long? ReadPeakWorkingSet() => Read(() => _process.PeakWorkingSet64);

    public TimeSpan? ReadCpuTime() => Read(() => _process.TotalProcessorTime);

    private static T? Read<T>(Func<T> read)
        where T : struct
    {
        try
        {
            return read();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }
}
