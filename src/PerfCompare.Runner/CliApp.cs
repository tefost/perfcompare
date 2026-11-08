using System.Text.Json;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public static class CliApp
{
    public const int ExitOk = 0;
    public const int ExitSendFailed = 1;
    public const int ExitUsage = 2;
    public const int ExitInternal = 3;
    public const int ExitInterrupted = 130;

    private const int SendAttempts = 3;

    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        HttpMessageHandler? handler = null,
        CancellationToken cancellation = default,
        TimeSpan? retryDelay = null)
    {
        if (!CliOptions.TryParse(args, out var options, out var parseError) || options is null)
        {
            await error.WriteLineAsync(parseError);
            await error.WriteLineAsync(CliOptions.Usage);
            return ExitUsage;
        }

        var delay = retryDelay ?? DefaultRetryDelay;
        try
        {
            return options.Command == CliOptions.SendCommand
                ? await SendSavedAsync(options, output, error, handler, delay)
                : await RunSeriesAsync(options, output, error, handler, cancellation, delay);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException or InvalidOperationException)
        {
            await error.WriteLineAsync($"Помилка виконання: {ex.Message}");
            return ExitInternal;
        }
    }

    private static async Task<int> RunSeriesAsync(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        HttpMessageHandler? handler,
        CancellationToken cancellation,
        TimeSpan delay)
    {
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
            using var client = new ResultClient(options.ServerUrl, handler);
            if (!await DeliverAsync(client, run, options, output, error, delay))
            {
                return ExitSendFailed;
            }
        }
        else if (options.OutPath is null)
        {
            await output.WriteLineAsync(JsonSerializer.Serialize(run, ContractJson.Options));
        }

        return interrupted ? ExitInterrupted : ExitOk;
    }

    private static async Task<bool> DeliverAsync(
        ResultClient client,
        RunResult run,
        CliOptions options,
        TextWriter output,
        TextWriter error,
        TimeSpan delay)
    {
        var runId = await SendWithRetryAsync(client, run, error, delay);
        if (runId is long id)
        {
            await output.WriteLineAsync($"Відправлено на сервер, runId={id}");
            return true;
        }

        var saved = ResultStore.Save(run, options.ResultsDir);
        await error.WriteLineAsync($"Не вдалося відправити результат. Збережено: {saved}");
        await error.WriteLineAsync($"Відправити пізніше: PerfCompare.Runner send {saved} --server {options.ServerUrl}");
        return false;
    }

    private static async Task<int> SendSavedAsync(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        HttpMessageHandler? handler,
        TimeSpan delay)
    {
        var files = ResultStore.ListPending(options.ScenarioPath);
        if (files is null)
        {
            await error.WriteLineAsync($"Шлях не знайдено: {options.ScenarioPath}");
            return ExitUsage;
        }

        if (files.Count == 0)
        {
            await output.WriteLineAsync("Немає файлів для відправки");
            return ExitOk;
        }

        using var client = new ResultClient(options.ServerUrl!, handler);
        var sent = 0;
        var failed = 0;
        foreach (var file in files)
        {
            var run = ResultStore.TryLoad(file);
            var runId = run is null ? null : await SendWithRetryAsync(client, run, error, delay);
            if (run is null)
            {
                await error.WriteLineAsync($"Пропущено, файл не є результатом: {file}");
            }

            if (runId is null)
            {
                failed++;
                continue;
            }

            ResultStore.MarkSent(file);
            sent++;
            await output.WriteLineAsync($"Відправлено {file}, runId={runId}");
        }

        await output.WriteLineAsync($"Відправлено: {sent}, не вдалося: {failed}");
        return failed == 0 ? ExitOk : ExitSendFailed;
    }

    private static async Task<long?> SendWithRetryAsync(
        ResultClient client, RunResult run, TextWriter error, TimeSpan delay)
    {
        for (var attempt = 1; attempt <= SendAttempts; attempt++)
        {
            try
            {
                return await client.SendAsync(run);
            }
            catch (ResultClientException ex)
            {
                await error.WriteLineAsync($"Спроба {attempt} з {SendAttempts}: {ex.Message}");
            }

            if (attempt < SendAttempts)
            {
                await Task.Delay(delay);
            }
        }

        return null;
    }
}
