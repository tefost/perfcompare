using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class CliOptionsSendTests
{
    [Fact]
    public void TryParse_Send_WithServer_Succeeds()
    {
        var ok = CliOptions.TryParse(new[] { "send", "results", "--server", "http://localhost:5000" }, out var options, out _);

        Assert.True(ok);
        Assert.Equal(CliOptions.SendCommand, options!.Command);
        Assert.Equal("results", options.ScenarioPath);
        Assert.Equal("http://localhost:5000", options.ServerUrl);
    }

    [Fact]
    public void TryParse_Send_WithoutServer_Fails()
    {
        var ok = CliOptions.TryParse(new[] { "send", "results" }, out var options, out var error);

        Assert.False(ok);
        Assert.Null(options);
        Assert.Contains("--server", error);
    }

    [Theory]
    [InlineData("--out", "x.json")]
    [InlineData("--results-dir", "saved")]
    public void TryParse_Send_RejectsRunOnlyOptions(string name, string value)
    {
        var args = new[] { "send", "results", "--server", "http://localhost:5000", name, value };

        var ok = CliOptions.TryParse(args, out _, out var error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryParse_Run_DefaultsResultsDir()
    {
        CliOptions.TryParse(new[] { "run", "s.yaml" }, out var options, out _);

        Assert.Equal(CliOptions.RunCommand, options!.Command);
        Assert.Equal(CliOptions.DefaultResultsDir, options.ResultsDir);
    }

    [Fact]
    public void TryParse_Run_AcceptsResultsDir()
    {
        var ok = CliOptions.TryParse(new[] { "run", "s.yaml", "--results-dir", "saved" }, out var options, out _);

        Assert.True(ok);
        Assert.Equal("saved", options!.ResultsDir);
    }

    [Fact]
    public void TryParse_ResultsDirWithoutValue_Fails()
    {
        var ok = CliOptions.TryParse(new[] { "run", "s.yaml", "--results-dir" }, out _, out var error);

        Assert.False(ok);
        Assert.Contains("--results-dir", error);
    }

    [Fact]
    public void Usage_MentionsBothCommands()
    {
        Assert.Contains("run <scenario.yaml>", CliOptions.Usage);
        Assert.Contains("send <файл|папка>", CliOptions.Usage);
    }
}
