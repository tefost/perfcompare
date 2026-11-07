using PerfCompare.Contracts;

namespace PerfCompare.Server.Database;

public sealed record EnvironmentLatestRun(
    long EnvironmentId,
    EnvironmentInfo Environment,
    long RunId,
    DateTimeOffset StartedAt);
