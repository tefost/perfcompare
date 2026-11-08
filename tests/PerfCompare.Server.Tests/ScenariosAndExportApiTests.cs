using System.Net;
using System.Net.Http.Json;
using PerfCompare.Contracts;
using PerfCompare.Server.Api;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Tests;

public class ScenariosAndExportApiTests
{
    [Fact]
    public async Task Scenarios_EmptyDatabase_ReturnsEmptyList()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var items = await client.GetFromJsonAsync<List<ScenarioItem>>(ApiRoutes.Scenarios, ContractJson.Options);

        Assert.Empty(items!);
    }

    [Fact]
    public async Task Scenarios_AfterTwoRuns_ReturnsBothInOrder()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunRepositoryTests.Sample("alpha"));
        await RunsApiTests.PostOk(client, RunRepositoryTests.Sample("beta"));

        var items = await client.GetFromJsonAsync<List<ScenarioItem>>(ApiRoutes.Scenarios, ContractJson.Options);

        Assert.Equal(new[] { "alpha", "beta" }, items!.Select(item => item.Name));
        Assert.Equal(new long[] { 1, 2 }, items.Select(item => item.Id));
    }

    [Fact]
    public async Task Export_ReturnsHeaderAndRowPerMeasurement()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunRepositoryTests.Sample());

        var response = await client.GetAsync(ApiRoutes.ExportUrl(1));
        var text = (await response.Content.ReadAsStringAsync()).TrimStart((char)0xFEFF);
        var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("runId,scenario,environmentId", lines[0]);
        Assert.Equal(4, lines.Length);
        Assert.StartsWith("1,baseline,1,Windows,10.0.22631,.NET 9.0.2,8,16384,", lines[1]);
        Assert.EndsWith(",1,812.4,41.2,790,0,Passed", lines[1]);
        Assert.EndsWith(",3,30000,,,,Timeout", lines[3]);
    }

    [Fact]
    public async Task Export_OnlyRequestedScenario()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunRepositoryTests.Sample("alpha"));
        await RunsApiTests.PostOk(client, RunRepositoryTests.Sample("beta"));

        var text = await client.GetStringAsync(ApiRoutes.ExportUrl(2));

        Assert.Contains(",beta,", text);
        Assert.DoesNotContain(",alpha,", text);
    }

    [Fact]
    public async Task Export_SetsAttachmentFileName()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunRepositoryTests.Sample());

        var response = await client.GetAsync(ApiRoutes.ExportUrl(1));

        Assert.Contains("perfcompare-scenario-1.csv", response.Content.Headers.ContentDisposition!.ToString());
    }

    [Fact]
    public async Task Export_UnknownScenario_Returns404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.ExportUrl(42));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_MissingScenarioId_Returns422()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.ExportCsv);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
