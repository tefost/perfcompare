using PerfCompare.Server.Database;

namespace PerfCompare.Server.Analytics;

public sealed class ComparisonService
{
    private readonly RunRepository _runs;
    private readonly BaselineRepository _baselines;

    public ComparisonService(RunRepository runs, BaselineRepository baselines)
    {
        _runs = runs;
        _baselines = baselines;
    }

    public ComparisonResult? Compare(long scenarioId, IReadOnlyCollection<long>? environmentIds)
    {
        var name = _baselines.GetScenarioName(scenarioId);
        if (name is null)
        {
            return null;
        }

        var items = new List<ComparisonItem>();
        foreach (var latest in _baselines.ListLatestRuns(scenarioId))
        {
            if (environmentIds is { Count: > 0 } && !environmentIds.Contains(latest.EnvironmentId))
            {
                continue;
            }

            var run = _runs.GetRun(latest.RunId);
            if (run is null)
            {
                continue;
            }

            items.Add(new ComparisonItem(
                latest.EnvironmentId,
                latest.Environment,
                latest.RunId,
                latest.StartedAt,
                _baselines.GetBaselineRunId(scenarioId, latest.EnvironmentId),
                RunStatsCalculator.Compute(run),
                null));
        }

        return new ComparisonResult(scenarioId, name, WithSlowdown(items));
    }

    private static List<ComparisonItem> WithSlowdown(List<ComparisonItem> items)
    {
        var medians = items
            .Where(item => item.Stats.Duration is not null)
            .Select(item => item.Stats.Duration!.Median)
            .ToList();
        if (medians.Count == 0 || medians.Min() <= 0)
        {
            return items;
        }

        var fastest = medians.Min();
        return items
            .Select(item => item.Stats.Duration is null
                ? item
                : item with { SlowdownVsFastest = Math.Round(item.Stats.Duration.Median / fastest, 2) })
            .ToList();
    }
}
