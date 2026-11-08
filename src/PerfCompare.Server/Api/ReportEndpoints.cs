using System.Globalization;
using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;

namespace PerfCompare.Server.Api;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.ReportHtml, Report);
        return app;
    }

    private static IResult Report(
        string? scenarioId, string? threshold, ComparisonService comparison, RegressionService regressions)
    {
        var errors = new List<string>();
        if (!long.TryParse(scenarioId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id < 1)
        {
            errors.Add("scenarioId: обов'язковий параметр, додатне число");
        }

        var limit = RegressionEndpoints.DefaultThresholdPercent;
        if (!string.IsNullOrWhiteSpace(threshold))
        {
            var parsed = double.TryParse(threshold, NumberStyles.Float, CultureInfo.InvariantCulture, out limit);
            if (!parsed || !(limit > 0 && limit <= RegressionEndpoints.MaxThresholdPercent))
            {
                errors.Add("threshold: число від 0 (не включно) до 1000 відсотків");
            }
        }

        if (errors.Count > 0)
        {
            return Results.UnprocessableEntity(new ErrorsResponse(errors));
        }

        var result = comparison.Compare(id, null);
        if (result is null)
        {
            return Results.NotFound(ErrorsResponse.Of($"сценарій {id} не знайдено"));
        }

        var reports = result.Items.ToDictionary(item => item.RunId, item => regressions.Check(item.RunId, limit));
        var html = HtmlReportBuilder.Build(result, reports, limit, DateTimeOffset.UtcNow);
        return Results.Text(html, "text/html; charset=utf-8");
    }
}
