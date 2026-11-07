using System.Net;
using System.Net.Http.Json;
using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;
using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class StatsApiTests
{
    [Fact]
    public async Task GetStats_ForSavedRun_ReturnsCounters()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var id = await RunsApiTests.PostOk(client, RunRepositoryTests.Sample());

        var stats = await client.GetFromJsonAsync<RunStats>(ApiRoutes.RunStatsLocation(id), ContractJson.Options);

        Assert.Equal(3, stats!.Total);
        Assert.Equal(1, stats.Passed);
        Assert.Equal(1, stats.Failed);
        Assert.Equal(1, stats.Timeouts);
        Assert.Equal(0.3333, stats.SuccessRate, 4);
        Assert.Equal(812.4, stats.Duration!.Median, 3);
    }

    [Fact]
    public async Task GetStats_UnknownRun_Returns404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.RunStatsLocation(404));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_ResponseUsesCamelCase()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var id = await RunsApiTests.PostOk(client, RunRepositoryTests.Sample());

        var text = await client.GetStringAsync(ApiRoutes.RunStatsLocation(id));

        Assert.Contains("\"successRate\"", text);
        Assert.Contains("\"stabilityIndex\"", text);
        Assert.Contains("\"cvPercent\"", text);
    }
}
