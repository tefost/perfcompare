using PerfCompare.Contracts;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class ProcessExecutorTests
{
    private static readonly string HelperDll =
        Path.Combine(AppContext.BaseDirectory, "PerfCompare.TestApp.dll");

    private static Scenario Helper(int timeoutSeconds, params string[] args) => new()
    {
        Name = "test",
        Command = "dotnet",
        Args = new[] { HelperDll }.Concat(args).ToList(),
        Repeats = 1,
        TimeoutSeconds = timeoutSeconds
    };

    [Fact]
    public void RunOnce_ExitZero_IsPassed()
    {
        var result = new ProcessExecutor().RunOnce(Helper(20, "exit", "0"));

        Assert.Equal(MeasurementStatus.Passed, result.Status);
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.DurationMs > 0);
    }

    [Fact]
    public void RunOnce_NonZeroExit_IsFailedWithCode()
    {
        var result = new ProcessExecutor().RunOnce(Helper(20, "exit", "3"));

        Assert.Equal(MeasurementStatus.Failed, result.Status);
        Assert.Equal(3, result.ExitCode);
    }

    [Fact]
    public void RunOnce_ExceedsTimeout_IsTimeoutAndProcessKilled()
    {
        var result = new ProcessExecutor().RunOnce(Helper(1, "sleep", "30"));

        Assert.Equal(MeasurementStatus.Timeout, result.Status);
        Assert.Null(result.ExitCode);
        Assert.InRange(result.DurationMs, 900, 15000);
    }

    [Fact]
    public void RunOnce_UnknownCommand_IsFailedWithError()
    {
        var scenario = new Scenario
        {
            Name = "test",
            Command = "no-such-command-" + Guid.NewGuid(),
            TimeoutSeconds = 5
        };

        var result = new ProcessExecutor().RunOnce(scenario);

        Assert.Equal(MeasurementStatus.Failed, result.Status);
        Assert.Null(result.ExitCode);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }
}
