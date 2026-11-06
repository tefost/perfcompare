using Microsoft.Data.Sqlite;
using PerfCompare.Contracts;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Tests;

public class RunRollbackTests
{
    private static RunResult WithInvalidMeasurement() => new(
        new ScenarioInfo("baseline", "dotnet", 2, 30),
        new EnvironmentInfo("Linux", "6.8", ".NET 9.0.2", 4, 8192),
        new DateTimeOffset(2026, 11, 7, 10, 15, 0, TimeSpan.Zero),
        new[]
        {
            new Measurement(1, 100, 10, 90, 0, MeasurementStatus.Passed),
            new Measurement(2, -1, 10, 90, 0, MeasurementStatus.Passed)
        });

    [Fact]
    public void SaveRun_InvalidMeasurement_ThrowsAndStoresNothing()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);

        Assert.Throws<SqliteException>(() => repository.SaveRun(WithInvalidMeasurement()));

        using var connection = db.Factory.Open();
        Assert.Equal(0L, TestDb.Scalar(connection, "SELECT COUNT(*) FROM runs;"));
        Assert.Equal(0L, TestDb.Scalar(connection, "SELECT COUNT(*) FROM measurements;"));
        Assert.Equal(0L, TestDb.Scalar(connection, "SELECT COUNT(*) FROM environments;"));
        Assert.Equal(0L, TestDb.Scalar(connection, "SELECT COUNT(*) FROM scenarios;"));
    }

    [Fact]
    public void SaveRun_DuplicateIteration_RollsBackWholeRun()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);
        var run = WithInvalidMeasurement() with
        {
            Measurements = new[]
            {
                new Measurement(1, 100, 10, 90, 0, MeasurementStatus.Passed),
                new Measurement(1, 120, 10, 90, 0, MeasurementStatus.Passed)
            }
        };

        Assert.Throws<SqliteException>(() => repository.SaveRun(run));

        Assert.Empty(repository.ListRuns());
    }

    [Fact]
    public void SaveRun_AfterFailedRun_StillWorks()
    {
        using var db = new TestDb();
        var repository = new RunRepository(db.Factory);
        Assert.Throws<SqliteException>(() => repository.SaveRun(WithInvalidMeasurement()));

        var id = repository.SaveRun(RunRepositoryTests.Sample());

        Assert.NotNull(repository.GetRun(id));
    }
}
