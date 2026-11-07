using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PerfCompare.Contracts;
using PerfCompare.Server.Api;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Tests;

public class RunsApiTests
{
    internal static async Task<HttpResponseMessage> Post(HttpClient client, RunResult run)
    {
        return await client.PostAsJsonAsync(ApiRoutes.Runs, run, ContractJson.Options);
    }

    internal static async Task<long> PostOk(HttpClient client, RunResult run)
    {
        var response = await Post(client, run);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RunCreated>(ContractJson.Options);
        return created!.RunId;
    }

    [Fact]
    public async Task Post_CorrectRun_Returns201WithRunId()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await Post(client, RunRepositoryTests.Sample());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RunCreated>(ContractJson.Options);
        Assert.Equal(1, created!.RunId);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Post_BrokenJson_Returns400()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiRoutes.Runs, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyBody_Returns400()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiRoutes.Runs, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownStatusText_Returns400()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var json = JsonSerializer.Serialize(RunRepositoryTests.Sample(), ContractJson.Options)
            .Replace("\"Passed\"", "\"Broken\"", StringComparison.Ordinal);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiRoutes.Runs, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidData_Returns422WithErrors()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var run = RunRepositoryTests.Sample();
        var invalid = run with { Scenario = run.Scenario with { Repeats = 0 } };

        var response = await Post(client, invalid);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>(ContractJson.Options);
        Assert.Contains(body!.Errors, e => e.StartsWith("scenario.repeats"));
    }

    [Fact]
    public async Task Post_MissingScenario_Returns422NotServerError()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("{\"measurements\":[]}", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiRoutes.Runs, content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidRun_IsNotStored()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var run = RunRepositoryTests.Sample();
        await Post(client, run with { Measurements = Array.Empty<Measurement>() });

        var list = await client.GetFromJsonAsync<List<RunSummary>>(ApiRoutes.Runs, ContractJson.Options);

        Assert.Empty(list!);
    }

    [Fact]
    public async Task Get_List_ReturnsSavedRuns()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await PostOk(client, RunRepositoryTests.Sample("a"));
        await PostOk(client, RunRepositoryTests.Sample("b"));

        var list = await client.GetFromJsonAsync<List<RunSummary>>(ApiRoutes.Runs, ContractJson.Options);

        Assert.Equal(2, list!.Count);
        Assert.Equal("b", list[0].Scenario);
    }

    [Fact]
    public async Task Get_ById_ReturnsTheSameRunThatWasPosted()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var sample = RunRepositoryTests.Sample();
        var id = await PostOk(client, sample);

        var loaded = await client.GetFromJsonAsync<RunResult>(ApiRoutes.RunLocation(id), ContractJson.Options);

        Assert.Equal(sample.Scenario, loaded!.Scenario);
        Assert.Equal(sample.Environment, loaded.Environment);
        Assert.Equal(3, loaded.Measurements.Count);
        Assert.Equal(MeasurementStatus.Timeout, loaded.Measurements[2].Status);
    }

    [Fact]
    public async Task Get_ById_Unknown_Returns404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.RunLocation(999));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ById_ResponseUsesCamelCaseAndEnumNames()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var id = await PostOk(client, RunRepositoryTests.Sample());

        var text = await client.GetStringAsync(ApiRoutes.RunLocation(id));

        Assert.Contains("\"timeoutSeconds\"", text);
        Assert.Contains("\"status\":\"Passed\"", text);
    }
}
