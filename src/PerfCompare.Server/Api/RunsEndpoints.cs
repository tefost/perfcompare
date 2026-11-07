using PerfCompare.Contracts;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Api;

public static class RunsEndpoints
{
    public static IEndpointRouteBuilder MapRunsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Runs, CreateRunAsync);
        app.MapGet(ApiRoutes.Runs, ListRuns);
        app.MapGet(ApiRoutes.RunById, GetRun);
        return app;
    }

    private static async Task<IResult> CreateRunAsync(
        HttpRequest request, RunRepository repository, CancellationToken cancellation)
    {
        var run = await RequestBody.ReadAsync<RunResult>(request, cancellation);
        if (run is null)
        {
            return Results.BadRequest(ErrorsResponse.Of("тіло запиту не є коректним JSON за контрактом"));
        }

        var errors = RunValidator.Validate(run);
        if (errors.Count > 0)
        {
            return Results.UnprocessableEntity(new ErrorsResponse(errors));
        }

        var runId = repository.SaveRun(run);
        return Results.Created(ApiRoutes.RunLocation(runId), new RunCreated(runId));
    }

    private static IResult ListRuns(RunRepository repository)
    {
        return Results.Json(repository.ListRuns(), ContractJson.Options);
    }

    private static IResult GetRun(long id, RunRepository repository)
    {
        var run = repository.GetRun(id);
        return run is null
            ? Results.NotFound(ErrorsResponse.Of($"запуск {id} не знайдено"))
            : Results.Json(run, ContractJson.Options);
    }
}
