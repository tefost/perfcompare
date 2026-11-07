using PerfCompare.Contracts;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Tests;

public class BaselineRepositoryTests
{
    private static long Count(TestDb db, string table)
    {
        using var connection = db.Factory.Open();
        return TestDb.Scalar(connection, $"SELECT COUNT(*) FROM {table};");
    }

    [Fact]
    public void SetBaseline_ForSavedRun_ReturnsIdAndStoresRun()
    {
        using var db = new TestDb();
        var runs = new RunRepository(db.Factory);
        var baselines = new BaselineRepository(db.Factory);
        var runId = runs.SaveRun(RunRepositoryTests.Sample());

        var baselineId = baselines.SetBaseline(runId);

        Assert.NotNull(baselineId);
        var scenarioId = runs.GetOrCreateScenario("baseline");
        var environmentId = runs.GetOrCreateEnvironment(RunRepositoryTests.Sample().Environment);
        Assert.Equal(runId, baselines.GetBaselineRunId(scenarioId, environmentId));
    }

    [Fact]
    public void SetBaseline_UnknownRun_ReturnsNullAndStoresNothing()
    {
        using var db = new TestDb();
        var baselines = new BaselineRepository(db.Factory);

        var result = baselines.SetBaseline(12345);

        Assert.Null(result);
        Assert.Equal(0, Count(db, "baselines"));
    }

    [Fact]
    public void SetBaseline_SecondRunOfSamePair_ReplacesRunInsteadOfAddingRow()
    {
        using var db = new TestDb();
        var runs = new RunRepository(db.Factory);
        var baselines = new BaselineRepository(db.Factory);
        var first = runs.SaveRun(RunRepositoryTests.Sample());
        var second = runs.SaveRun(RunRepositoryTests.Sample());

        var firstBaseline = baselines.SetBaseline(first);
        var secondBaseline = baselines.SetBaseline(second);

        Assert.Equal(firstBaseline, secondBaseline);
        Assert.Equal(1, Count(db, "baselines"));
        var scenarioId = runs.GetOrCreateScenario("baseline");
        var environmentId = runs.GetOrCreateEnvironment(RunRepositoryTests.Sample().Environment);
        Assert.Equal(second, baselines.GetBaselineRunId(scenarioId, environmentId));
    }

    [Fact]
    public void SetBaseline_DifferentEnvironments_KeepSeparateBaselines()
    {
        using var db = new TestDb();
        var runs = new RunRepository(db.Factory);
        var baselines = new BaselineRepository(db.Factory);
        var eight = runs.SaveRun(RunRepositoryTests.Sample(cores: 8));
        var four = runs.SaveRun(RunRepositoryTests.Sample(cores: 4));

        baselines.SetBaseline(eight);
        baselines.SetBaseline(four);

        Assert.Equal(2, Count(db, "baselines"));
    }

    [Fact]
    public void GetBaselineRunId_NoBaseline_ReturnsNull()
    {
        using var db = new TestDb();
        var baselines = new BaselineRepository(db.Factory);

        Assert.Null(baselines.GetBaselineRunId(1, 1));
    }

    [Fact]
    public void GetScenarioName_ReturnsNameOrNull()
    {
        using var db = new TestDb();
        var runs = new RunRepository(db.Factory);
        var baselines = new BaselineRepository(db.Factory);
        var id = runs.GetOrCreateScenario("startup");

        Assert.Equal("startup", baselines.GetScenarioName(id));
        Assert.Null(baselines.GetScenarioName(id + 100));
    }

    [Fact]
    public void ListLatestRuns_ReturnsNewestRunPerEnvironment()
    {
        using var db = new TestDb();
        var runs = new RunRepository(db.Factory);
        var baselines = new BaselineRepository(db.Factory);
        runs.SaveRun(RunRepositoryTests.Sample(cores: 8));
        var newestEight = runs.SaveRun(RunRepositoryTests.Sample(cores: 8));
        var four = runs.SaveRun(RunRepositoryTests.Sample(cores: 4));
        var scenarioId = runs.GetOrCreateScenario("baseline");

        var latest = baselines.ListLatestRuns(scenarioId);

        Assert.Equal(2, latest.Count);
        Assert.Equal(newestEight, latest[0].RunId);
        Assert.Equal(8, latest[0].Environment.Cores);
        Assert.Equal(four, latest[1].RunId);
    }

    [Fact]
    public void ListLatestRuns_OtherScenarioRuns_AreNotIncluded()
    {
        using var db = new TestDb();
        var runs = new RunRepository(db.Factory);
        var baselines = new BaselineRepository(db.Factory);
        runs.SaveRun(RunRepositoryTests.Sample("a"));
        runs.SaveRun(RunRepositoryTests.Sample("b"));

        var latest = baselines.ListLatestRuns(runs.GetOrCreateScenario("a"));

        Assert.Single(latest);
    }

    [Fact]
    public void ListLatestRuns_ScenarioWithoutRuns_IsEmpty()
    {
        using var db = new TestDb();
        var runs = new RunRepository(db.Factory);
        var baselines = new BaselineRepository(db.Factory);
        var scenarioId = runs.GetOrCreateScenario("empty");

        Assert.Empty(baselines.ListLatestRuns(scenarioId));
    }
}
