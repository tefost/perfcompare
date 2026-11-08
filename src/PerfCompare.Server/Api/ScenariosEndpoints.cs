using PerfCompare.Contracts;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Api;

public static class ScenariosEndpoints
{
    public static IEndpointRouteBuilder MapScenariosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Scenarios, List);
        return app;
    }

    private static IResult List(ScenarioRepository repository)
    {
        return Results.Json(repository.List(), ContractJson.Options);
    }
}
