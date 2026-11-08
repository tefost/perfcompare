using System.Net;
using System.Text;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class CliOfflineTests
{
    private const string Server = "http://localhost:5000";

    private static HttpResponseMessage Created(int runId) => new(HttpStatusCode.Created)
    {
        Content = new StringContent($"{{\"runId\":{runId}}}", Encoding.UTF8, "application/json")
    };

    private static async Task<(int Code, string Out, string Err)> Run(string[] args, HttpMessageHandler? handler)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = await CliApp.RunAsync(args, output, error, handler, default, TimeSpan.Zero);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public async Task Run_ServerDown_TriesThreeTimesThenSavesResult()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0");
            var saved = Path.Combine(dir, "saved");
            var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

            var result = await Run(new[] { "run", scenario, "--server", Server, "--results-dir", saved }, handler);

            Assert.Equal(CliApp.ExitSendFailed, result.Code);
            Assert.Equal(3, handler.Uris.Count);
            Assert.Single(Directory.GetFiles(saved, "*.json"));
            Assert.Contains("Спроба 3 з 3", result.Err);
            Assert.Contains("Збережено", result.Err);
            Assert.Contains("send", result.Err);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task Run_ServerRecoversOnSecondAttempt_SendsWithoutSaving()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0");
            var saved = Path.Combine(dir, "saved");
            var calls = 0;
            var handler = new StubHandler(_ => ++calls == 1
                ? throw new HttpRequestException("тимчасово")
                : Created(5));

            var result = await Run(new[] { "run", scenario, "--server", Server, "--results-dir", saved }, handler);

            Assert.Equal(CliApp.ExitOk, result.Code);
            Assert.Equal(2, handler.Uris.Count);
            Assert.Contains("runId=5", result.Out);
            Assert.False(Directory.Exists(saved));
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task Run_ServerAnswersWithError_IsRetriedAndSaved()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0");
            var saved = Path.Combine(dir, "saved");
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            var result = await Run(new[] { "run", scenario, "--server", Server, "--results-dir", saved }, handler);

            Assert.Equal(CliApp.ExitSendFailed, result.Code);
            Assert.Equal(3, handler.Uris.Count);
            Assert.Single(Directory.GetFiles(saved, "*.json"));
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task Run_SavedFile_CanBeSentLaterWithSendCommand()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0");
            var saved = Path.Combine(dir, "saved");
            var down = new StubHandler(_ => throw new HttpRequestException("недоступний"));
            await Run(new[] { "run", scenario, "--server", Server, "--results-dir", saved }, down);
            var up = new StubHandler(_ => Created(9));

            var result = await Run(new[] { "send", saved, "--server", Server }, up);

            Assert.Equal(CliApp.ExitOk, result.Code);
            Assert.Single(up.Bodies);
            Assert.Contains("runId=9", result.Out);
            Assert.Empty(Directory.GetFiles(saved, "*.json"));
            Assert.Single(Directory.GetFiles(saved, "*.json.sent"));
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task Send_Directory_SendsEveryFileAndSecondCallHasNothingToDo()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            ResultStore.Save(CliTestHelpers.SampleRun("a", minute: 1), dir);
            ResultStore.Save(CliTestHelpers.SampleRun("b", minute: 2), dir);
            var handler = new StubHandler(_ => Created(1));

            var first = await Run(new[] { "send", dir, "--server", Server }, handler);
            var second = await Run(new[] { "send", dir, "--server", Server }, handler);

            Assert.Equal(CliApp.ExitOk, first.Code);
            Assert.Equal(2, handler.Bodies.Count);
            Assert.Contains("Відправлено: 2, не вдалося: 0", first.Out);
            Assert.Contains("Немає файлів", second.Out);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task Send_ServerDown_KeepsFilesAndReturnsOne()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = ResultStore.Save(CliTestHelpers.SampleRun(), dir);
            var handler = new StubHandler(_ => throw new HttpRequestException("недоступний"));

            var result = await Run(new[] { "send", dir, "--server", Server }, handler);

            Assert.Equal(CliApp.ExitSendFailed, result.Code);
            Assert.True(File.Exists(path));
            Assert.Contains("не вдалося: 1", result.Out);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task Send_BrokenFile_IsSkippedAndOthersStillSent()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            ResultStore.Save(CliTestHelpers.SampleRun("good"), dir);
            await File.WriteAllTextAsync(Path.Combine(dir, "aaa-broken.json"), "це не json");
            var handler = new StubHandler(_ => Created(3));

            var result = await Run(new[] { "send", dir, "--server", Server }, handler);

            Assert.Equal(CliApp.ExitSendFailed, result.Code);
            Assert.Single(handler.Bodies);
            Assert.Contains("файл не є результатом", result.Err);
            Assert.Contains("Відправлено: 1, не вдалося: 1", result.Out);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task Send_MissingPath_ReturnsUsageCode()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var result = await Run(new[] { "send", missing, "--server", Server }, new StubHandler(_ => Created(1)));

        Assert.Equal(CliApp.ExitUsage, result.Code);
        Assert.Contains("Шлях не знайдено", result.Err);
    }

    [Fact]
    public async Task Send_WithoutServer_ReturnsUsageCode()
    {
        var result = await Run(new[] { "send", "results" }, null);

        Assert.Equal(CliApp.ExitUsage, result.Code);
        Assert.Contains("Використання", result.Err);
    }
}
