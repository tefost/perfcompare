using System.ComponentModel;
using System.Diagnostics;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public sealed class ProcessExecutor
{
    private readonly TimeSpan _killGrace;

    public ProcessExecutor(TimeSpan? killGrace = null)
    {
        _killGrace = killGrace ?? TimeSpan.FromSeconds(5);
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
        process.WaitForExit();
        timer.Stop();

        var status = process.ExitCode == 0 ? MeasurementStatus.Passed : MeasurementStatus.Failed;
        return new ProcessResult(timer.Elapsed.TotalMilliseconds, process.ExitCode, status);
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
