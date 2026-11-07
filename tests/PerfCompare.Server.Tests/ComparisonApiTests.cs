using System.Net;
using System.Net.Http.Json;
using System.Text;
using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;
using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class ComparisonApiTests
{
    private static RunResult RunWithDuration(int cores, double duration)
    {
        return RunRepositoryTests.Sample(cores: cores) with
        {
            Measurements = new[] { new Measurement(1, duration, 10, 5, 0, MeasurementStatus.Passed) }
        };
    }

    private static async Task<long> PostBaseline(HttpClient client, long runId)
    {
        var response = await client.PostAsJsonAsync(ApiRoutes.Baselines, new BaselineRequest(runId), ContractJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<BaselineCreated>(ContractJson.Options);
        return created!.BaselineId;
    }

    private static async Task<ComparisonResult> Compare(HttpClient client, string? environmentIds = null)
    {
        var result = await client.GetFromJsonAsync<ComparisonResult>(
            ApiRoutes.CompareUrl(1, environmentIds), ContractJson.Options);
        return result!;
    }

    [Fact]
    public async Task Compare_TwoEnvironments_ReturnsItemPerEnvironmentWithSlowdown()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunWithDuration(8, 100));
        await RunsApiTests.PostOk(client, RunWithDuration(4, 300));

        var result = await Compare(client);

        Assert.Equal("baseline", result.Scenario);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1.0, result.Items[0].SlowdownVsFastest!.Value, 3);
        Assert.Equal(3.0, result.Items[1].SlowdownVsFastest!.Value, 3);
    }

    [Fact]
    public async Task Compare_UsesLatestRunOfEachEnvironment()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunWithDuration(8, 100));
        var newest = await RunsApiTests.PostOk(client, RunWithDuration(8, 200));

        var result = await Compare(client);

        Assert.Single(result.Items);
        Assert.Equal(newest, result.Items[0].RunId);
        Assert.Equal(200.0, result.Items[0].Stats.Duration!.Median, 3);
    }

    [Fact]
    public async Task Compare_EnvironmentFilter_LimitsItems()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunWithDuration(8, 100));
        await RunsApiTests.PostOk(client, RunWithDuration(4, 300));

        var result = await Compare(client, "2");

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].EnvironmentId);
        Assert.Equal(4, result.Items[0].Environment.Cores);
    }

    [Fact]
    public async Task Compare_UnknownScenario_Returns404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.CompareUrl(99));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Compare_MissingScenarioId_Returns422()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.Compare);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>(ContractJson.Options);
        Assert.Contains(body!.Errors, e => e.StartsWith("scenarioId"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1,x")]
    [InlineData("0")]
    [InlineData("-3")]
    public async Task Compare_BadEnvironmentIds_Returns422(string environmentIds)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunWithDuration(8, 100));

        var response = await client.GetAsync(ApiRoutes.CompareUrl(1, environmentIds));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Baseline_Set_AppearsInComparison()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var eight = await RunsApiTests.PostOk(client, RunWithDuration(8, 100));
        await RunsApiTests.PostOk(client, RunWithDuration(4, 300));

        await PostBaseline(client, eight);
        var result = await Compare(client);

        Assert.Equal(eight, result.Items[0].BaselineRunId);
        Assert.Null(result.Items[1].BaselineRunId);
    }

    [Fact]
    public async Task Baseline_SetAgain_ReplacesRunAndKeepsId()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var first = await RunsApiTests.PostOk(client, RunWithDuration(8, 100));
        var second = await RunsApiTests.PostOk(client, RunWithDuration(8, 110));

        var firstId = await PostBaseline(client, first);
        var secondId = await PostBaseline(client, second);
        var result = await Compare(client);

        Assert.Equal(firstId, secondId);
        Assert.Equal(second, result.Items[0].BaselineRunId);
    }

    [Fact]
    public async Task Baseline_UnknownRun_Returns422()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(ApiRoutes.Baselines, new BaselineRequest(777), ContractJson.Options);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Baseline_NonPositiveRunId_Returns422()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(ApiRoutes.Baselines, new BaselineRequest(0), ContractJson.Options);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Baseline_BrokenJson_Returns400()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("{ runId:", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiRoutes.Baselines, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
