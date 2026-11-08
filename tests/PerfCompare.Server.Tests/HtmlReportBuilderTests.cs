using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;
using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class HtmlReportBuilderTests
{
    private static readonly DateTimeOffset Moment = new(2026, 11, 9, 10, 30, 0, TimeSpan.Zero);

    private static ComparisonItem Item(long runId, double? median, double? slowdown, string os = "Windows")
    {
        var duration = median is double value ? new DurationStats(value, value, value, value, value, 0, 0) : null;
        var stats = new RunStats(3, 3, 0, 0, 1, duration, 10, 5, 0.9);
        var environment = new EnvironmentInfo(os, "11", ".NET 9", 8, 16000);
        return new ComparisonItem(runId, environment, runId, Moment, null, stats, slowdown);
    }

    private static string Build(string scenario, IReadOnlyList<ComparisonItem> items, Dictionary<long, RegressionReport?>? reports = null)
    {
        var comparison = new ComparisonResult(1, scenario, items);
        return HtmlReportBuilder.Build(comparison, reports ?? new Dictionary<long, RegressionReport?>(), 10, Moment);
    }

    [Fact]
    public void Build_EscapesScenarioName()
    {
        var html = Build("<script>alert(1)</script>", Array.Empty<ComparisonItem>());

        Assert.DoesNotContain("<script>alert", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_EscapesEnvironmentFields()
    {
        var html = Build("s", new[] { Item(1, 100, 1, os: "<b>Evil</b>") });

        Assert.DoesNotContain("<b>Evil</b>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;Evil&lt;/b&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithoutRuns_ShowsMessageInsteadOfTable()
    {
        var html = Build("s", Array.Empty<ComparisonItem>());

        Assert.Contains("ще немає запусків", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<table>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithRuns_ShowsMedianAndSlowdown()
    {
        var html = Build("s", new[] { Item(1, 100, 1), Item(2, 250.5, 2.5) });

        Assert.Contains("<td>250.5</td>", html, StringComparison.Ordinal);
        Assert.Contains("<td>2.5x</td>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithoutDuration_ShowsDash()
    {
        var html = Build("s", new[] { Item(1, null, null) });

        Assert.Contains("<td>-</td>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ShowsRegressionAndMissingBaseline()
    {
        var regression = new RegressionReport(
            1, 9, RegressionStatus.Regression, 10,
            new[] { new MetricChange("median", 100, 140, 40, true) });
        var reports = new Dictionary<long, RegressionReport?>
        {
            [1] = regression,
            [2] = new RegressionReport(2, null, RegressionStatus.NoBaseline, 10, Array.Empty<MetricChange>())
        };

        var html = Build("s", new[] { Item(1, 140, 1.4), Item(2, 100, 1) }, reports);

        Assert.Contains("регресія +40%", html, StringComparison.Ordinal);
        Assert.Contains("немає базової лінії", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_UsesInvariantCultureAndUtcTimestamp()
    {
        var html = Build("s", Array.Empty<ComparisonItem>());

        Assert.Contains("2026-11-09 10:30 UTC", html, StringComparison.Ordinal);
    }
}
