using System.Net;
using System.Net.Http.Json;
using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;
using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class RegressionApiTests
{
    private static RunResult RunWithDuration(double duration, int cores = 8)
    {
        return RunRepositoryTests.Sample(cores: cores) with
        {
            Measurements = new[] { new Measurement(1, duration, 10, 5, 0, MeasurementStatus.Passed) }
        };
    }

    private static async Task SetBaseline(HttpClient client, long runId)
    {
        var response = await client.PostAsJsonAsync(ApiRoutes.Baselines, new BaselineRequest(runId), ContractJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<RegressionReport> Check(HttpClient client, long runId, string? threshold = null)
    {
        var report = await client.GetFromJsonAsync<RegressionReport>(
            ApiRoutes.RegressionsUrl(runId, threshold), ContractJson.Options);
        return report!;
    }

    [Fact]
    public async Task Check_WithoutBaseline_ReturnsNoBaseline()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var runId = await RunsApiTests.PostOk(client, RunWithDuration(100));

        var report = await Check(client, runId);

        Assert.Equal(RegressionStatus.NoBaseline, report.Status);
        Assert.Null(report.BaselineRunId);
        Assert.Empty(report.Metrics);
    }

    [Fact]
    public async Task Check_SlowerThanBaseline_ReturnsRegression()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var baselineId = await RunsApiTests.PostOk(client, RunWithDuration(100));
        await SetBaseline(client, baselineId);
        var runId = await RunsApiTests.PostOk(client, RunWithDuration(130));

        var report = await Check(client, runId);

        Assert.Equal(RegressionStatus.Regression, report.Status);
        Assert.Equal(baselineId, report.BaselineRunId);
        Assert.Equal(10.0, report.ThresholdPercent, 6);
        var median = report.Metrics.Single(metric => metric.Metric == RegressionMetrics.DurationMedian);
        Assert.Equal(30.0, median.ChangePercent, 6);
        Assert.True(median.IsRegression);
    }

    [Fact]
    public async Task Check_WithinThreshold_ReturnsOk()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await SetBaseline(client, await RunsApiTests.PostOk(client, RunWithDuration(100)));
        var runId = await RunsApiTests.PostOk(client, RunWithDuration(105));

        var report = await Check(client, runId);

        Assert.Equal(RegressionStatus.Ok, report.Status);
    }

    [Fact]
    public async Task Check_CustomThreshold_AcceptsLargerGrowth()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await SetBaseline(client, await RunsApiTests.PostOk(client, RunWithDuration(100)));
        var runId = await RunsApiTests.PostOk(client, RunWithDuration(130));

        var report = await Check(client, runId, "50");

        Assert.Equal(RegressionStatus.Ok, report.Status);
        Assert.Equal(50.0, report.ThresholdPercent, 6);
    }

    [Fact]
    public async Task Check_BaselineRunAgainstItself_ReturnsOk()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var baselineId = await RunsApiTests.PostOk(client, RunWithDuration(100));
        await SetBaseline(client, baselineId);

        var report = await Check(client, baselineId);

        Assert.Equal(RegressionStatus.Ok, report.Status);
        Assert.All(report.Metrics, metric => Assert.Equal(0.0, metric.ChangePercent, 6));
    }

    [Fact]
    public async Task Check_BaselineOfAnotherEnvironment_DoesNotApply()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await SetBaseline(client, await RunsApiTests.PostOk(client, RunWithDuration(100, cores: 8)));
        var runId = await RunsApiTests.PostOk(client, RunWithDuration(500, cores: 4));

        var report = await Check(client, runId);

        Assert.Equal(RegressionStatus.NoBaseline, report.Status);
    }

    [Fact]
    public async Task Check_UnknownRun_Returns404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.RegressionsUrl(99));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Check_MissingRunId_Returns422()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.Regressions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1001")]
    [InlineData("NaN")]
    public async Task Check_InvalidThreshold_Returns422(string threshold)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var runId = await RunsApiTests.PostOk(client, RunWithDuration(100));

        var response = await client.GetAsync(ApiRoutes.RegressionsUrl(runId, threshold));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Check_ResponseUsesCamelCaseAndTextStatus()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var runId = await RunsApiTests.PostOk(client, RunWithDuration(100));

        var text = await client.GetStringAsync(ApiRoutes.RegressionsUrl(runId));

        Assert.Contains("\"status\":\"no_baseline\"", text);
        Assert.Contains("\"thresholdPercent\":10", text);
    }
}
