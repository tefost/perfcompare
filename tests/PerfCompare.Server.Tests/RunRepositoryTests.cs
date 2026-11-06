using PerfCompare.Contracts;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Tests;

public class RunRepositoryTests
{
    internal static RunResult Sample(string scenario = "baseline", int cores = 8) => new(
        new ScenarioInfo(scenario, "dotnet", 3, 30),
        new EnvironmentInfo("Windows", "10.0.22631", ".NET 9.0.2", cores, 16384),
        new DateTimeOffset(2026, 11, 7, 10, 15, 0, TimeSpan.Zero),
        new[]
        {
            new Measurement(1, 812.4, 41.2, 790.0, 0, MeasurementStatus.Passed),
            new Measurement(2, 900.1, null, null, 3, MeasurementStatus.Failed),
            new Measurement(3, 30000, null, null, null, MeasurementStatus.Timeout)
        });

    [Fact]
    public void GetOrCreateEnvironment_SameEnvironment_ReturnsSameId()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);
        var environment = Sample().Environment;

        var first = repository.GetOrCreateEnvironment(environment);
        var second = repository.GetOrCreateEnvironment(environment);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetOrCreateEnvironment_DifferentCores_CreatesNewRow()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);

        var first = repository.GetOrCreateEnvironment(Sample(cores: 8).Environment);
        var second = repository.GetOrCreateEnvironment(Sample(cores: 4).Environment);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetOrCreateScenario_SameName_ReturnsSameId()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);

        var first = repository.GetOrCreateScenario("baseline");
        var second = repository.GetOrCreateScenario("baseline");
        var other = repository.GetOrCreateScenario("slow");

        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void SaveRun_ThenGetRun_ReturnsSameData()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);
        var run = Sample();

        var id = repository.SaveRun(run);
        var loaded = repository.GetRun(id);

        Assert.NotNull(loaded);
        Assert.Equal(run.Scenario, loaded!.Scenario);
        Assert.Equal(run.Environment, loaded.Environment);
        Assert.Equal(run.StartedAt, loaded.StartedAt);
        Assert.Equal(3, loaded.Measurements.Count);
        Assert.Equal(run.Measurements[0], loaded.Measurements[0]);
        Assert.Null(loaded.Measurements[1].PeakMemMb);
        Assert.Equal(MeasurementStatus.Timeout, loaded.Measurements[2].Status);
        Assert.Null(loaded.Measurements[2].ExitCode);
    }

    [Fact]
    public void SaveRun_TwoRunsOfSameScenario_ShareScenarioAndEnvironment()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);

        repository.SaveRun(Sample());
        repository.SaveRun(Sample());
        using var connection = db.Factory.Open();

        Assert.Equal(1L, TestDb.Scalar(connection, "SELECT COUNT(*) FROM scenarios;"));
        Assert.Equal(1L, TestDb.Scalar(connection, "SELECT COUNT(*) FROM environments;"));
        Assert.Equal(2L, TestDb.Scalar(connection, "SELECT COUNT(*) FROM runs;"));
    }

    [Fact]
    public void ListRuns_ReturnsNewestFirstWithMeasurementCount()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);
        var first = repository.SaveRun(Sample("baseline"));
        var second = repository.SaveRun(Sample("slow"));

        var runs = repository.ListRuns();

        Assert.Equal(2, runs.Count);
        Assert.Equal(second, runs[0].Id);
        Assert.Equal("slow", runs[0].Scenario);
        Assert.Equal(first, runs[1].Id);
        Assert.Equal(3, runs[1].Measurements);
    }

    [Fact]
    public void ListRuns_EmptyDatabase_ReturnsEmptyList()
    {
        using var db = new TestDb();

        Assert.Empty(new RunRepository(db.Factory).ListRuns());
    }

    [Fact]
    public void GetRun_UnknownId_ReturnsNull()
    {
        using var db = new TestDb();

        Assert.Null(new RunRepository(db.Factory).GetRun(12345));
    }
}
