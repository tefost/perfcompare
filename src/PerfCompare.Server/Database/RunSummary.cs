namespace PerfCompare.Server.Database;

public sealed record RunSummary(
    long Id,
    string Scenario,
    long EnvironmentId,
    string Os,
    DateTimeOffset StartedAt,
    int Measurements);
