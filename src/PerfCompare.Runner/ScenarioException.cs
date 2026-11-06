namespace PerfCompare.Runner;

public sealed class ScenarioException : Exception
{
    public ScenarioException()
    {
    }

    public ScenarioException(string message)
        : base(message)
    {
    }

    public ScenarioException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
