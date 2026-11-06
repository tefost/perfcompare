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

        if (!process.WaitForExit(TimeSpan.FromSeconds(scenario.TimeoutSeconds)))
        {
            KillTree(process);
            timer.Stop();
            return new ProcessResult(timer.Elapsed.TotalMilliseconds, null, MeasurementStatus.Timeout);
        }

        process.WaitForExit();
        timer.Stop();

        var status = process.ExitCode == 0 ? MeasurementStatus.Passed : MeasurementStatus.Failed;
        return new ProcessResult(timer.Elapsed.TotalMilliseconds, process.ExitCode, status);
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
