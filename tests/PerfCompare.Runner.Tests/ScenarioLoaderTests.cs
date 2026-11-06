using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class ScenarioLoaderTests
{
    private const string Valid = """
        name: baseline
        command: dotnet
        args: ["--version"]
        repeats: 5
        timeoutSeconds: 30
        warmup: 1
        """;

    [Fact]
    public void Parse_ValidYaml_ReturnsScenario()
    {
        var scenario = ScenarioLoader.Parse(Valid);

        Assert.Equal("baseline", scenario.Name);
        Assert.Equal("dotnet", scenario.Command);
        Assert.Equal(new[] { "--version" }, scenario.Args);
        Assert.Equal(5, scenario.Repeats);
        Assert.Equal(30, scenario.TimeoutSeconds);
        Assert.Equal(1, scenario.Warmup);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void Parse_RepeatsOutOfRange_Throws(int repeats)
    {
        var yaml = $"""
            name: x
            command: dotnet
            repeats: {repeats}
            """;

        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.Parse(yaml));
        Assert.Contains("repeats", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Parse_RepeatsAtBoundary_IsAccepted(int repeats)
    {
        var yaml = $"""
            name: x
            command: dotnet
            repeats: {repeats}
            """;

        var scenario = ScenarioLoader.Parse(yaml);

        Assert.Equal(repeats, scenario.Repeats);
        Assert.Equal(0, scenario.Warmup);
    }

    [Fact]
    public void Parse_TimeoutZero_Throws()
    {
        var yaml = """
            name: x
            command: dotnet
            timeoutSeconds: 0
            """;

        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.Parse(yaml));
        Assert.Contains("timeoutSeconds", ex.Message);
    }

    [Fact]
    public void Parse_MissingCommand_Throws()
    {
        var yaml = """
            name: x
            repeats: 3
            """;

        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.Parse(yaml));
        Assert.Contains("command", ex.Message);
    }

    [Fact]
    public void Parse_SeveralErrors_AreReportedTogether()
    {
        var yaml = """
            name: ""
            command: ""
            repeats: 0
            """;

        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.Parse(yaml));

        Assert.Contains("name", ex.Message);
        Assert.Contains("command", ex.Message);
        Assert.Contains("repeats", ex.Message);
    }

    [Fact]
    public void Parse_EmptyYaml_Throws()
    {
        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.Parse(""));

        Assert.Contains("порожній", ex.Message);
    }

    [Fact]
    public void Parse_BrokenYaml_ThrowsReadableMessage()
    {
        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.Parse("name: [unclosed"));

        Assert.Contains("YAML", ex.Message);
    }

    [Fact]
    public void Parse_UnknownKey_Throws()
    {
        var yaml = """
            name: x
            command: dotnet
            repets: 5
            """;

        Assert.Throws<ScenarioException>(() => ScenarioLoader.Parse(yaml));
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".yaml");

        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.Load(path));

        Assert.Contains("не знайдено", ex.Message);
    }

    [Fact]
    public void Load_ReadsFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".yaml");
        File.WriteAllText(path, Valid);

        try
        {
            var scenario = ScenarioLoader.Load(path);

            Assert.Equal("baseline", scenario.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
