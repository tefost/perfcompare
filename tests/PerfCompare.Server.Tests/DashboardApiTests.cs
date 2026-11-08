using System.Net;
using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class DashboardApiTests
{
    [Fact]
    public async Task Root_ReturnsHtmlPage()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.Separator);
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("PerfCompare", text);
        Assert.Contains("id=\"scenario\"", text);
    }

    [Fact]
    public async Task EveryAsset_IsServedWithContent()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        foreach (var name in DashboardEndpoints.AssetNames)
        {
            var response = await client.GetAsync(ApiRoutes.Separator + name);
            var bytes = await response.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(bytes);
        }
    }

    [Fact]
    public async Task ChartLibrary_IsServedLocally()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.Separator + "chart.umd.js");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal("text/javascript", response.Content.Headers.ContentType!.MediaType);
        Assert.True(bytes.Length > 100_000, "бібліотека графіків має бути повною");
    }

    [Fact]
    public async Task UnknownPath_Returns404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.Separator + "missing.js");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApiRoutes_StillWorkNextToDashboard()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.Runs);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
