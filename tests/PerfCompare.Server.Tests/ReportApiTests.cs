using System.Net;
using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class ReportApiTests
{
    [Fact]
    public async Task Report_ForExistingScenario_ReturnsHtml()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunRepositoryTests.Sample("baseline"));

        var response = await client.GetAsync(ApiRoutes.ReportUrl(1));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("<table>", html, StringComparison.Ordinal);
        Assert.Contains("baseline", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Report_WithCustomThreshold_ShowsIt()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RunsApiTests.PostOk(client, RunRepositoryTests.Sample("baseline"));

        var html = await client.GetStringAsync(ApiRoutes.ReportUrl(1, "25"));

        Assert.Contains("Поріг регресії: 25%", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/report.html")]
    [InlineData("/api/report.html?scenarioId=abc")]
    [InlineData("/api/report.html?scenarioId=0")]
    [InlineData("/api/report.html?scenarioId=1&threshold=0")]
    [InlineData("/api/report.html?scenarioId=1&threshold=5000")]
    [InlineData("/api/report.html?scenarioId=1&threshold=abc")]
    public async Task Report_WithBadParameters_Returns422(string url)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Report_ForUnknownScenario_Returns404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.ReportUrl(99));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_LinksToReport()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var page = await client.GetStringAsync(ApiRoutes.Separator);
        var script = await client.GetStringAsync("app.js");

        Assert.Contains("id=\"report\"", page, StringComparison.Ordinal);
        Assert.Contains("api/report.html", script, StringComparison.Ordinal);
    }
}
