using System.Globalization;
using PerfCompare.Contracts;
using PerfCompare.Server.Analytics;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Api;

public static class ComparisonEndpoints
{
    public static IEndpointRouteBuilder MapComparisonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Baselines, CreateBaselineAsync);
        app.MapGet(ApiRoutes.Compare, Compare);
        return app;
    }

    private static async Task<IResult> CreateBaselineAsync(
        HttpRequest request, BaselineRepository repository, CancellationToken cancellation)
    {
        var body = await RequestBody.ReadAsync<BaselineRequest>(request, cancellation);
        if (body is null)
        {
            return Results.BadRequest(ErrorsResponse.Of("тіло запиту не є коректним JSON"));
        }

        if (body.RunId < 1)
        {
            return Results.UnprocessableEntity(ErrorsResponse.Of("runId: має бути додатним числом"));
        }

        var baselineId = repository.SetBaseline(body.RunId);
        return baselineId is null
            ? Results.UnprocessableEntity(ErrorsResponse.Of($"runId: запуск {body.RunId} не знайдено"))
            : Results.Created(ApiRoutes.Baselines, new BaselineCreated(baselineId.Value));
    }

    private static IResult Compare(string? scenarioId, string? environmentIds, ComparisonService service)
    {
        var errors = new List<string>();
        var scenarioOk = TryParseId(scenarioId, out var scenario);
        if (!scenarioOk)
        {
            errors.Add("scenarioId: обов'язковий параметр, додатне число");
        }

        var environments = ParseIds(environmentIds, errors);
        if (errors.Count > 0)
        {
            return Results.UnprocessableEntity(new ErrorsResponse(errors));
        }

        var result = service.Compare(scenario, environments);
        return result is null
            ? Results.NotFound(ErrorsResponse.Of($"сценарій {scenario} не знайдено"))
            : Results.Json(result, ContractJson.Options);
    }

    private static List<long>? ParseIds(string? raw, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var ids = new List<long>();
        foreach (var part in raw.Split(','))
        {
            if (!TryParseId(part.Trim(), out var id))
            {
                errors.Add("environmentIds: очікується список додатних чисел через кому");
                return null;
            }

            ids.Add(id);
        }

        return ids;
    }

    private static bool TryParseId(string? text, out long id)
    {
        return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id >= 1;
    }
}
