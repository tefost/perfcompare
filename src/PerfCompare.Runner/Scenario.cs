namespace PerfCompare.Runner;

public sealed class Scenario
{
    public string Name { get; set; } = "";

    public string Command { get; set; } = "";

    public List<string> Args { get; set; } = new();

    public int Repeats { get; set; } = 1;

    public int TimeoutSeconds { get; set; } = 30;

    public int Warmup { get; set; }

    public string? WorkingDirectory { get; set; }
}
