using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using PerfCompare.Contracts;
using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class EdgeCaseTests
{
    private static RunResult Boundary(
        int repeats = 3,
        int timeout = 30,
        string name = "baseline",
        int cores = 8,
        double duration = 100,
        int measurements = 1)
    {
        var sample = RunRepositoryTests.Sample(name);
        return sample with
        {
            Scenario = sample.Scenario with { Repeats = repeats, TimeoutSeconds = timeout },
            Environment = sample.Environment with { Cores = cores },
            Measurements = Enumerable.Range(1, measurements)
                .Select(iteration => new Measurement(iteration, duration, 10, 5, 0, MeasurementStatus.Passed))
                .ToList()
        };
    }

    private static async Task<HttpResponseMessage> PostRaw(HttpClient client, string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await client.PostAsync(ApiRoutes.Runs, content);
    }

    // Case 1: input validation

    [Fact]
    public async Task Validation_InvalidRun_Returns422ListingEveryProblem()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var run = Boundary(repeats: 0, name: " ", cores: 0);

        var response = await RunsApiTests.Post(client, run);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>(ContractJson.Options);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(body!.Errors, error => error.StartsWith("scenario.name", StringComparison.Ordinal));
        Assert.Contains(body.Errors, error => error.StartsWith("scenario.repeats", StringComparison.Ordinal));
        Assert.Contains(body.Errors, error => error.StartsWith("environment.cores", StringComparison.Ordinal));
        var stored = await client.GetFromJsonAsync<List<object>>(ApiRoutes.Runs, ContractJson.Options);
        Assert.Empty(stored!);
    }

    // Case 2: empty and malformed data

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("\"text\"")]
    [InlineData("123")]
    [InlineData("{\"scenario\":\"not an object\"}")]
    public async Task MalformedBody_Returns400(string body)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await PostRaw(client, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmptyObject_Returns422WithErrors()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await PostRaw(client, "{}");
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>(ContractJson.Options);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEmpty(body!.Errors);
    }

    [Theory]
    [InlineData("/api/compare")]
    [InlineData("/api/compare?scenarioId=abc")]
    [InlineData("/api/compare?scenarioId=1&environmentIds=x,2")]
    [InlineData("/api/regressions")]
    [InlineData("/api/regressions?runId=-1")]
    [InlineData("/api/regressions?runId=1&threshold=abc")]
    [InlineData("/api/export.csv")]
    [InlineData("/api/export.csv?scenarioId=0")]
    public async Task GarbageQuery_Returns422(string url)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/runs/abc")]
    [InlineData("/api/runs/0/stats")]
    [InlineData("/missing")]
    public async Task UnknownPath_Returns404(string url)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Case 3: exceptions without a crash

    [Fact]
    public async Task DatabaseFailure_Returns500JsonWithoutDetails_AndServerKeepsRunning()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, Boundary());
        File.Delete(factory.DatabasePath);

        var failed = await client.GetAsync(ApiRoutes.Runs);
        var text = await failed.Content.ReadAsStringAsync();
        var alive = await client.GetAsync(ApiRoutes.Separator);

        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        Assert.Equal("application/json", failed.Content.Headers.ContentType!.MediaType);
        Assert.Contains("\"errors\"", text);
        Assert.DoesNotContain("no such table", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, alive.StatusCode);
    }

    [Fact]
    public async Task DatabaseFailure_IsLoggedAsError()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, Boundary());
        File.Delete(factory.DatabasePath);

        await client.GetAsync(ApiRoutes.Runs);

        Assert.Contains(
            factory.Logs.Entries,
            entry => entry.Category == ErrorHandling.Category && entry.Level == LogLevel.Error && entry.Exception is not null);
    }

    // Case 4: boundary values

    [Theory]
    [InlineData(1, 30, 1)]
    [InlineData(100, 3600, 1)]
    [InlineData(5, 1, 5)]
    public async Task BoundaryValues_AreAccepted(int repeats, int timeout, int measurements)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await RunsApiTests.Post(client, Boundary(repeats, timeout, measurements: measurements));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task BoundaryValues_LongestNameZeroDurationAndOneCore_AreAccepted()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await RunsApiTests.Post(client, Boundary(name: new string('a', 100), cores: 1, duration: 0));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 30, 1)]
    [InlineData(101, 30, 1)]
    [InlineData(3, 0, 1)]
    [InlineData(3, 3601, 1)]
    [InlineData(5, 30, 6)]
    public async Task OutsideBoundary_IsRejected(int repeats, int timeout, int measurements)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await RunsApiTests.Post(client, Boundary(repeats, timeout, measurements: measurements));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task OutsideBoundary_NameOf101CharactersAndNegativeDuration_AreRejected()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var longName = await RunsApiTests.Post(client, Boundary(name: new string('a', 101)));
        var negative = await RunsApiTests.Post(client, Boundary(duration: -0.1));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, longName.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, negative.StatusCode);
    }

    // Case 5: no unhandled exceptions in the log

    [Fact]
    public async Task BadRequests_LeaveNoErrorsInLog()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        await PostRaw(client, "{");
        await PostRaw(client, string.Empty);
        await PostRaw(client, "{}");
        await RunsApiTests.Post(client, Boundary(repeats: 0));
        await client.GetAsync("/api/runs/abc");
        await client.GetAsync("/api/runs/999");
        await client.GetAsync("/api/compare?scenarioId=abc");
        await client.GetAsync("/api/compare?scenarioId=99");
        await client.GetAsync("/api/regressions?runId=-1");
        await client.GetAsync("/api/export.csv?scenarioId=99");
        await client.GetAsync("/missing");

        Assert.DoesNotContain(factory.Logs.Entries, entry => entry.Level >= LogLevel.Error);
        Assert.DoesNotContain(factory.Logs.Entries, entry => entry.Exception is not null);
    }

    [Fact]
    public async Task NormalWork_LeavesNoErrorsInLog()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var runId = await RunsApiTests.PostOk(client, Boundary());
        await client.GetAsync(ApiRoutes.Runs);
        await client.GetAsync(ApiRoutes.RunStatsLocation(runId));
        await client.GetAsync(ApiRoutes.CompareUrl(1));
        await client.GetAsync(ApiRoutes.RegressionsUrl(runId));
        await client.GetAsync(ApiRoutes.ExportUrl(1));

        Assert.DoesNotContain(factory.Logs.Entries, entry => entry.Level >= LogLevel.Error);
    }
}
