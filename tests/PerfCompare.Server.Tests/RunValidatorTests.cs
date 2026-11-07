using PerfCompare.Contracts;
using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class RunValidatorTests
{
    private static RunResult Valid() => RunRepositoryTests.Sample();

    private static RunResult WithScenario(Func<ScenarioInfo, ScenarioInfo> change)
    {
        var run = Valid();
        return run with { Scenario = change(run.Scenario) };
    }

    private static RunResult WithEnvironment(Func<EnvironmentInfo, EnvironmentInfo> change)
    {
        var run = Valid();
        return run with { Environment = change(run.Environment) };
    }

    private static RunResult WithMeasurements(params Measurement[] measurements)
    {
        return Valid() with { Measurements = measurements };
    }

    [Fact]
    public void Validate_CorrectRun_HasNoErrors()
    {
        Assert.Empty(RunValidator.Validate(Valid()));
    }

    [Fact]
    public void Validate_Null_ReturnsError()
    {
        Assert.NotEmpty(RunValidator.Validate(null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void Validate_RepeatsOutOfRange_NamesTheField(int repeats)
    {
        var errors = RunValidator.Validate(WithScenario(s => s with { Repeats = repeats }));

        Assert.Contains(errors, e => e.StartsWith("scenario.repeats"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_RepeatsAtBoundary_IsAccepted(int repeats)
    {
        var run = WithScenario(s => s with { Repeats = repeats }) with
        {
            Measurements = new[] { new Measurement(1, 10, null, null, 0, MeasurementStatus.Passed) }
        };

        Assert.Empty(RunValidator.Validate(run));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    public void Validate_TimeoutOutOfRange_NamesTheField(int timeout)
    {
        var errors = RunValidator.Validate(WithScenario(s => s with { TimeoutSeconds = timeout }));

        Assert.Contains(errors, e => e.StartsWith("scenario.timeoutSeconds"));
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var errors = RunValidator.Validate(WithScenario(s => s with { Name = "  " }));

        Assert.Contains(errors, e => e.StartsWith("scenario.name"));
    }

    [Fact]
    public void Validate_NameLongerThan100_ReturnsError()
    {
        var errors = RunValidator.Validate(WithScenario(s => s with { Name = new string('a', 101) }));

        Assert.Contains(errors, e => e.StartsWith("scenario.name"));
    }

    [Fact]
    public void Validate_EmptyCommand_ReturnsError()
    {
        var errors = RunValidator.Validate(WithScenario(s => s with { Command = string.Empty }));

        Assert.Contains(errors, e => e.StartsWith("scenario.command"));
    }

    [Fact]
    public void Validate_ZeroCores_ReturnsError()
    {
        var errors = RunValidator.Validate(WithEnvironment(e => e with { Cores = 0 }));

        Assert.Contains(errors, e => e.StartsWith("environment.cores"));
    }

    [Fact]
    public void Validate_ZeroRam_ReturnsError()
    {
        var errors = RunValidator.Validate(WithEnvironment(e => e with { RamMb = 0 }));

        Assert.Contains(errors, e => e.StartsWith("environment.ramMb"));
    }

    [Fact]
    public void Validate_EmptyOsFields_ReturnsErrorForEach()
    {
        var errors = RunValidator.Validate(WithEnvironment(e => e with { Os = "", OsVersion = "", Runtime = "" }));

        Assert.Contains(errors, e => e.StartsWith("environment.os:"));
        Assert.Contains(errors, e => e.StartsWith("environment.osVersion"));
        Assert.Contains(errors, e => e.StartsWith("environment.runtime"));
    }

    [Fact]
    public void Validate_DefaultStartedAt_ReturnsError()
    {
        var errors = RunValidator.Validate(Valid() with { StartedAt = default });

        Assert.Contains(errors, e => e.StartsWith("startedAt"));
    }

    [Fact]
    public void Validate_NoMeasurements_ReturnsError()
    {
        var errors = RunValidator.Validate(WithMeasurements());

        Assert.Contains(errors, e => e.StartsWith("measurements"));
    }

    [Fact]
    public void Validate_MoreMeasurementsThanRepeats_ReturnsError()
    {
        var run = WithScenario(s => s with { Repeats = 1 });

        var errors = RunValidator.Validate(run);

        Assert.Contains(errors, e => e.Contains("repeats = 1"));
    }

    [Fact]
    public void Validate_DuplicateIteration_ReturnsError()
    {
        var errors = RunValidator.Validate(WithMeasurements(
            new Measurement(1, 10, null, null, 0, MeasurementStatus.Passed),
            new Measurement(1, 12, null, null, 0, MeasurementStatus.Passed)));

        Assert.Contains(errors, e => e.StartsWith("measurements[1].iteration"));
    }

    [Fact]
    public void Validate_IterationZero_ReturnsError()
    {
        var errors = RunValidator.Validate(WithMeasurements(
            new Measurement(0, 10, null, null, 0, MeasurementStatus.Passed)));

        Assert.Contains(errors, e => e.StartsWith("measurements[0].iteration"));
    }

    [Fact]
    public void Validate_NegativeDuration_ReturnsError()
    {
        var errors = RunValidator.Validate(WithMeasurements(
            new Measurement(1, -1, null, null, 0, MeasurementStatus.Passed)));

        Assert.Contains(errors, e => e.StartsWith("measurements[0].durationMs"));
    }

    [Fact]
    public void Validate_NegativeMemoryOrCpu_ReturnsErrors()
    {
        var errors = RunValidator.Validate(WithMeasurements(
            new Measurement(1, 10, -1, -2, 0, MeasurementStatus.Passed)));

        Assert.Contains(errors, e => e.StartsWith("measurements[0].peakMemMb"));
        Assert.Contains(errors, e => e.StartsWith("measurements[0].cpuTimeMs"));
    }

    [Fact]
    public void Validate_NullMemoryAndCpu_AreAllowed()
    {
        var errors = RunValidator.Validate(WithMeasurements(
            new Measurement(1, 10, null, null, null, MeasurementStatus.Timeout)));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_UnknownStatus_ReturnsError()
    {
        var errors = RunValidator.Validate(WithMeasurements(
            new Measurement(1, 10, null, null, 0, (MeasurementStatus)99)));

        Assert.Contains(errors, e => e.StartsWith("measurements[0].status"));
    }

    [Fact]
    public void Validate_SeveralProblems_ReportsAllOfThem()
    {
        var run = WithScenario(s => s with { Repeats = 0, Name = "" })
            with { Environment = Valid().Environment with { Cores = 0 } };

        var errors = RunValidator.Validate(run);

        Assert.True(errors.Count >= 3);
    }
}
