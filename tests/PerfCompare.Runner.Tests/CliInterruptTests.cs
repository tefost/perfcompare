using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class CliInterruptTests
{
    [Fact]
    public async Task Run_AlreadyCancelled_ReturnsInterruptedAndWritesNoFile()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0", repeats: 3);
            var outPath = Path.Combine(dir, "result.json");
            using var source = new CancellationTokenSource();
            await source.CancelAsync();
            var output = new StringWriter();
            var error = new StringWriter();

            var code = await CliApp.RunAsync(new[] { "run", scenario, "--out", outPath }, output, error, null, source.Token);

            Assert.Equal(CliApp.ExitInterrupted, code);
            Assert.Contains("перервано", error.ToString());
            Assert.False(File.Exists(outPath));
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task Run_NotCancelled_StillReturnsOk()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0");
            var outPath = Path.Combine(dir, "result.json");

            var code = await CliApp.RunAsync(
                new[] { "run", scenario, "--out", outPath }, new StringWriter(), new StringWriter());

            Assert.Equal(CliApp.ExitOk, code);
            Assert.True(File.Exists(outPath));
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void ExitCodes_AreDistinct()
    {
        var codes = new[] { CliApp.ExitOk, CliApp.ExitSendFailed, CliApp.ExitUsage, CliApp.ExitInterrupted };

        Assert.Equal(codes.Length, codes.Distinct().Count());
    }
}
