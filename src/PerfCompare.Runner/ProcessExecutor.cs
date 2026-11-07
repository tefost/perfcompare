using System.ComponentModel;
using System.Diagnostics;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public sealed class ProcessExecutor
{
    private readonly TimeSpan _killGrace;
    private readonly TimeSpan _sampleInterval;
    private readonly Func<Process, IMetricsProvider> _providerFactory;

    public ProcessExecutor(
        TimeSpan? killGrace = null,
        TimeSpan? sampleInterval = null,
        Func<Process, IMetricsProvider>? providerFactory = null)
    {
        _killGrace = killGrace ?? TimeSpan.FromSeconds(5);
        _sampleInterval = sampleInterval ?? TimeSpan.FromMilliseconds(20);
        _providerFactory = providerFactory ?? (static process => new ProcessMetricsProvider(process));
    }

    public ProcessResult RunOnce(Scenario scenario)
    {
        using var process = new Process { StartInfo = BuildStartInfo(scenario) };
        process.OutputDataReceived += static (_, _) => { };
        process.ErrorDataReceived += static (_, _) => { };

        var timer = Stopwatch.StartNew();
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            return new ProcessResult(0, null, MeasurementStatus.Failed, ex.Message);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var sampler = new MetricsSampler();
        var provider = _providerFactory(process);
        var limit = TimeSpan.FromSeconds(scenario.TimeoutSeconds);

        if (!WaitWithSampling(process, provider, sampler, timer, limit))
        {
            KillTree(process);
            timer.Stop();
            return new ProcessResult(
                timer.Elapsed.TotalMilliseconds, null, MeasurementStatus.Timeout, null, sampler.PeakMemMb, sampler.CpuTimeMs);
        }

        process.WaitForExit();
        timer.Stop();
        sampler.Sample(provider);

        var status = process.ExitCode == 0 ? MeasurementStatus.Passed : MeasurementStatus.Failed;
        return new ProcessResult(
            timer.Elapsed.TotalMilliseconds, process.ExitCode, status, null, sampler.PeakMemMb, sampler.CpuTimeMs);
    }

    private bool WaitWithSampling(
        Process process, IMetricsProvider provider, MetricsSampler sampler, Stopwatch timer, TimeSpan limit)
    {
        while (true)
        {
            sampler.Sample(provider);

            var remaining = limit - timer.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            var step = remaining < _sampleInterval ? remaining : _sampleInterval;
            if (process.WaitForExit(step))
            {
                return true;
            }
        }
    }

    private void KillTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(_killGrace);
        }
        catch (InvalidOperationException)
        {
            // already exited
        }
    }

    private static ProcessStartInfo BuildStartInfo(Scenario scenario)
    {
        var info = new ProcessStartInfo(scenario.Command)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in scenario.Args)
        {
            info.ArgumentList.Add(arg);
        }

        if (!string.IsNullOrWhiteSpace(scenario.WorkingDirectory))
        {
            info.WorkingDirectory = scenario.WorkingDirectory;
        }

        return info;
    }
}
