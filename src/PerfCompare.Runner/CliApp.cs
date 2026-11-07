using System.Text.Json;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public static class CliApp
{
    public const int ExitOk = 0;
    public const int ExitSendFailed = 1;
    public const int ExitUsage = 2;
    public const int ExitInterrupted = 130;

    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        HttpMessageHandler? handler = null,
        CancellationToken cancellation = default)
    {
        if (!CliOptions.TryParse(args, out var options, out var parseError) || options is null)
        {
            await error.WriteLineAsync(parseError);
            await error.WriteLineAsync(CliOptions.Usage);
            return ExitUsage;
        }

        Scenario scenario;
        try
        {
            scenario = ScenarioLoader.Load(options.ScenarioPath);
        }
        catch (ScenarioException ex)
        {
            await error.WriteLineAsync($"Помилка сценарію: {ex.Message}");
            return ExitUsage;
        }

        var run = SeriesRunner.Run(scenario, new ProcessExecutor(), EnvironmentFingerprint.Capture(), output, cancellation);
        if (run.Measurements.Count == 0)
        {
            await error.WriteLineAsync("Серію перервано до першого вимірювання, результату немає");
            return ExitInterrupted;
        }

        var interrupted = cancellation.IsCancellationRequested;
        if (interrupted)
        {
            await output.WriteLineAsync(
                $"Серію перервано: виконано {run.Measurements.Count} з {scenario.Repeats} вимірювань");
        }

        if (options.OutPath is not null)
        {
            ResultFile.Write(options.OutPath, run);
            await output.WriteLineAsync($"Результат збережено: {options.OutPath}");
        }

        if (options.ServerUrl is not null)
        {
            try
            {
                using var client = new ResultClient(options.ServerUrl, handler);
                var runId = await client.SendAsync(run);
                await output.WriteLineAsync($"Відправлено на сервер, runId={runId}");
            }
            catch (ResultClientException ex)
            {
                await error.WriteLineAsync($"Не вдалося відправити результат: {ex.Message}");
                return ExitSendFailed;
            }
        }

        if (options.OutPath is null && options.ServerUrl is null)
        {
            await output.WriteLineAsync(JsonSerializer.Serialize(run, ContractJson.Options));
        }

        return interrupted ? ExitInterrupted : ExitOk;
    }
}
