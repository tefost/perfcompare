using PerfCompare.Contracts;

namespace PerfCompare.Server.Analytics;

public sealed record ComparisonItem(
    long EnvironmentId,
    EnvironmentInfo Environment,
    long RunId,
    DateTimeOffset StartedAt,
    long? BaselineRunId,
    RunStats Stats,
    double? SlowdownVsFastest);

public sealed record ComparisonResult(
    long ScenarioId,
    string Scenario,
    IReadOnlyList<ComparisonItem> Items);

public sealed record BaselineRequest(long RunId);

public sealed record BaselineCreated(long BaselineId);
