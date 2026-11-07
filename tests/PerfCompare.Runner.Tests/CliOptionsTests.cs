using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class CliOptionsTests
{
    [Fact]
    public void TryParse_ScenarioOnly_Succeeds()
    {
        var ok = CliOptions.TryParse(new[] { "run", "s.yaml" }, out var options, out _);

        Assert.True(ok);
        Assert.Equal("s.yaml", options!.ScenarioPath);
        Assert.Null(options.ServerUrl);
        Assert.Null(options.OutPath);
    }

    [Fact]
    public void TryParse_AllOptions_Succeeds()
    {
        var args = new[] { "run", "s.yaml", "--server", "http://localhost:5000", "--out", "r.json" };

        var ok = CliOptions.TryParse(args, out var options, out _);

        Assert.True(ok);
        Assert.Equal("http://localhost:5000", options!.ServerUrl);
        Assert.Equal("r.json", options.OutPath);
    }

    [Theory]
    [InlineData]
    [InlineData("run")]
    [InlineData("start", "s.yaml")]
    public void TryParse_MissingOrWrongCommand_Fails(params string[] args)
    {
        var ok = CliOptions.TryParse(args, out var options, out var error);

        Assert.False(ok);
        Assert.Null(options);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("--server")]
    [InlineData("--server", "not-a-url")]
    [InlineData("--server", "ftp://host")]
    [InlineData("--out")]
    [InlineData("--unknown", "x")]
    public void TryParse_BadOption_Fails(params string[] extra)
    {
        var args = new[] { "run", "s.yaml" }.Concat(extra).ToArray();

        var ok = CliOptions.TryParse(args, out _, out var error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
