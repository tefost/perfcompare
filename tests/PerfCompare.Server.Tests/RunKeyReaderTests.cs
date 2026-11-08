using PerfCompare.Server.Database;

namespace PerfCompare.Server.Tests;

public class RunKeyReaderTests
{
    [Fact]
    public void Find_SavedRun_ReturnsScenarioAndEnvironment()
    {
        using var db = new TestDb();
        var runs = new RunRepository(db.Factory);
        var sample = RunRepositoryTests.Sample();
        var runId = runs.SaveRun(sample);

        var key = new RunKeyReader(db.Factory).Find(runId);

        Assert.NotNull(key);
        Assert.Equal(runs.GetOrCreateScenario("baseline"), key.ScenarioId);
        Assert.Equal(runs.GetOrCreateEnvironment(sample.Environment), key.EnvironmentId);
    }

    [Fact]
    public void Find_UnknownRun_ReturnsNull()
    {
        using var db = new TestDb();

        Assert.Null(new RunKeyReader(db.Factory).Find(999));
    }
}
