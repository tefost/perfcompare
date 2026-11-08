using PerfCompare.Server.Database;

namespace PerfCompare.Server.Analytics;

public sealed class RegressionService
{
    private readonly RunRepository _runs;
    private readonly BaselineRepository _baselines;
    private readonly RunKeyReader _keys;

    public RegressionService(RunRepository runs, BaselineRepository baselines, RunKeyReader keys)
    {
        _runs = runs;
        _baselines = baselines;
        _keys = keys;
    }

    public RegressionReport? Check(long runId, double thresholdPercent)
    {
        var run = _runs.GetRun(runId);
        var key = _keys.Find(runId);
        if (run is null || key is null)
        {
            return null;
        }

        var baselineId = _baselines.GetBaselineRunId(key.ScenarioId, key.EnvironmentId);
        var baseline = baselineId is long id ? _runs.GetRun(id) : null;
        if (baselineId is null || baseline is null)
        {
            return new RegressionReport(runId, null, RegressionStatus.NoBaseline, thresholdPercent, Array.Empty<MetricChange>());
        }

        var changes = RegressionDetector.Detect(
            RunStatsCalculator.Compute(baseline), RunStatsCalculator.Compute(run), thresholdPercent);
        var status = changes.Any(change => change.IsRegression) ? RegressionStatus.Regression : RegressionStatus.Ok;
        return new RegressionReport(runId, baselineId, status, thresholdPercent, changes);
    }
}
