using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Api;

public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.RunStats, GetStats);
        return app;
    }

    private static IResult GetStats(long id, RunRepository repository)
    {
        var run = repository.GetRun(id);
        return run is null
            ? Results.NotFound(ErrorsResponse.Of($"запуск {id} не знайдено"))
            : Results.Json(RunStatsCalculator.Compute(run), ContractJson.Options);
    }
}
