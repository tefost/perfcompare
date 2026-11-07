using System.Net;
using System.Text;
using PerfCompare.Contracts;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class ResultClientTests
{
    private static RunResult Sample() => new(
        new ScenarioInfo("baseline", "dotnet --version", 1, 30),
        new EnvironmentInfo("Linux", "6.8", ".NET 9.0.2", 4, 8192),
        new DateTimeOffset(2026, 11, 7, 10, 15, 0, TimeSpan.Zero),
        new[] { new Measurement(1, 50, 10, 40, 0, MeasurementStatus.Passed) });

    private static HttpResponseMessage Json(HttpStatusCode code, string body) => new(code)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task SendAsync_Created_ReturnsRunId()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.Created, "{\"runId\":7}"));
        using var client = new ResultClient("http://localhost:5000", handler);

        var id = await client.SendAsync(Sample());

        Assert.Equal(7, id);
        Assert.Equal("http://localhost:5000/api/runs", handler.Uris[0]!.ToString());
        Assert.Contains("\"timeoutSeconds\":30", handler.Bodies[0]);
        Assert.Contains("\"status\":\"Passed\"", handler.Bodies[0]);
    }

    [Fact]
    public async Task SendAsync_ServerTrailingSlash_StillPostsToApiRuns()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.Created, "{\"runId\":1}"));
        using var client = new ResultClient("http://localhost:5000/", handler);

        await client.SendAsync(Sample());

        Assert.Equal("http://localhost:5000/api/runs", handler.Uris[0]!.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendAsync_ErrorStatus_ThrowsWithStatusCode(HttpStatusCode code)
    {
        var handler = new StubHandler(_ => Json(code, "{\"errors\":[\"x\"]}"));
        using var client = new ResultClient("http://localhost:5000", handler);

        var ex = await Assert.ThrowsAsync<ResultClientException>(() => client.SendAsync(Sample()));

        Assert.Contains(((int)code).ToString(), ex.Message);
    }

    [Fact]
    public async Task SendAsync_ServerUnavailable_ThrowsReadableException()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));
        using var client = new ResultClient("http://localhost:5000", handler);

        var ex = await Assert.ThrowsAsync<ResultClientException>(() => client.SendAsync(Sample()));

        Assert.Contains("недоступний", ex.Message);
    }

    [Fact]
    public async Task SendAsync_EmptyBody_Throws()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.Created, "null"));
        using var client = new ResultClient("http://localhost:5000", handler);

        await Assert.ThrowsAsync<ResultClientException>(() => client.SendAsync(Sample()));
    }
}
