using System.Globalization;
using System.Text;
using PerfCompare.Contracts;
using PerfCompare.Server.Database;

namespace PerfCompare.Server.Api;

public static class ExportEndpoints
{
    private const char ByteOrderMark = (char)0xFEFF;

    private const string Header =
        "runId,scenario,environmentId,os,osVersion,runtime,cores,ramMb,startedAt,iteration,durationMs,peakMemMb,cpuTimeMs,exitCode,status";

    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.ExportCsv, Export);
        return app;
    }

    private static IResult Export(
        string? scenarioId, RunRepository runs, BaselineRepository baselines, HttpResponse response)
    {
        if (!long.TryParse(scenarioId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id < 1)
        {
            return Results.UnprocessableEntity(ErrorsResponse.Of("scenarioId: обов'язковий параметр, додатне число"));
        }

        var name = baselines.GetScenarioName(id);
        if (name is null)
        {
            return Results.NotFound(ErrorsResponse.Of($"сценарій {id} не знайдено"));
        }

        var builder = new StringBuilder().Append(ByteOrderMark);
        builder.Append(Header).Append(CsvWriter.NewLine);
        foreach (var summary in runs.ListRuns().Where(item => item.Scenario == name).OrderBy(item => item.Id))
        {
            var run = runs.GetRun(summary.Id);
            if (run is null)
            {
                continue;
            }

            foreach (var measurement in run.Measurements.OrderBy(item => item.Iteration))
            {
                builder.Append(Row(summary, run, measurement)).Append(CsvWriter.NewLine);
            }
        }

        response.Headers.ContentDisposition = $"attachment; filename=perfcompare-scenario-{id}.csv";
        return Results.Text(builder.ToString(), "text/csv; charset=utf-8");
    }

    private static string Row(RunSummary summary, RunResult run, Measurement measurement)
    {
        var fields = new[]
        {
            CsvWriter.Integer(summary.Id),
            CsvWriter.Field(run.Scenario.Name),
            CsvWriter.Integer(summary.EnvironmentId),
            CsvWriter.Field(run.Environment.Os),
            CsvWriter.Field(run.Environment.OsVersion),
            CsvWriter.Field(run.Environment.Runtime),
            CsvWriter.Integer(run.Environment.Cores),
            CsvWriter.Integer(run.Environment.RamMb),
            run.StartedAt.ToString("O", CultureInfo.InvariantCulture),
            CsvWriter.Integer(measurement.Iteration),
            CsvWriter.Number(measurement.DurationMs),
            CsvWriter.Number(measurement.PeakMemMb),
            CsvWriter.Number(measurement.CpuTimeMs),
            CsvWriter.Integer(measurement.ExitCode),
            measurement.Status.ToString()
        };
        return string.Join(",", fields);
    }
}
