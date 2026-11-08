using System.Globalization;
using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;

namespace PerfCompare.Server.Api;

public static class RegressionEndpoints
{
    public const double DefaultThresholdPercent = 10;
    public const double MaxThresholdPercent = 1000;

    public static IEndpointRouteBuilder MapRegressionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Regressions, Check);
        return app;
    }

    private static IResult Check(string? runId, string? threshold, RegressionService service)
    {
        var errors = new List<string>();
        if (!long.TryParse(runId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id < 1)
        {
            errors.Add("runId: обов'язковий параметр, додатне число");
        }

        var limit = DefaultThresholdPercent;
        if (!string.IsNullOrWhiteSpace(threshold))
        {
            var parsed = double.TryParse(threshold, NumberStyles.Float, CultureInfo.InvariantCulture, out limit);
            if (!parsed || !(limit > 0 && limit <= MaxThresholdPercent))
            {
                errors.Add("threshold: число від 0 (не включно) до 1000 відсотків");
            }
        }

        if (errors.Count > 0)
        {
            return Results.UnprocessableEntity(new ErrorsResponse(errors));
        }

        var report = service.Check(id, limit);
        return report is null
            ? Results.NotFound(ErrorsResponse.Of($"запуск {id} не знайдено"))
            : Results.Json(report, ContractJson.Options);
    }
}
