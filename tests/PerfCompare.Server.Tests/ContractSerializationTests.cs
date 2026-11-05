using System.Text.Json;
using PerfCompare.Contracts;

namespace PerfCompare.Server.Tests;

public class ContractSerializationTests
{
    private static RunResult Sample() => new(
        new ScenarioInfo("baseline", "samples/CpuBound", 5, 30),
        new EnvironmentInfo("Windows", "10.0.22631", ".NET 9.0.2", 8, 16384),
        new DateTimeOffset(2026, 11, 7, 10, 15, 0, TimeSpan.Zero),
        new[] { new Measurement(1, 812.4, 41.2, 790.0, 0, MeasurementStatus.Passed) });

    [Fact]
    public void RunResult_RoundTrip_KeepsData()
    {
        var json = JsonSerializer.Serialize(Sample(), ContractJson.Options);
        var back = JsonSerializer.Deserialize<RunResult>(json, ContractJson.Options);

        Assert.NotNull(back);
        Assert.Equal("baseline", back!.Scenario.Name);
        Assert.Equal(MeasurementStatus.Passed, back.Measurements[0].Status);
    }

    [Fact]
    public void Json_UsesCamelCaseAndEnumStrings()
    {
        var json = JsonSerializer.Serialize(Sample(), ContractJson.Options);

        Assert.Contains("\"timeoutSeconds\":30", json);
        Assert.Contains("\"status\":\"Passed\"", json);
    }

    [Fact]
    public void Measurement_NullMetrics_AreAllowed()
    {
        var m = new Measurement(1, 100, null, null, null, MeasurementStatus.Timeout);
        var json = JsonSerializer.Serialize(m, ContractJson.Options);
        var back = JsonSerializer.Deserialize<Measurement>(json, ContractJson.Options);

        Assert.Null(back!.PeakMemMb);
        Assert.Null(back.ExitCode);
    }
}
