using System.Diagnostics;
using System.Net;
using System.Text;

namespace PerfCompare.Runner.Tests;

public class RunnerEdgeCaseTests
{
    private const string TraceMarker = "   at ";
    private const string CrashMarker = "Unhandled exception";

    private static async Task<(int Code, string Output, string Error)> Invoke(
        string[] args, HttpMessageHandler? handler = null)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = await CliApp.RunAsync(args, output, error, handler, retryDelay: TimeSpan.Zero);
        return (code, output.ToString(), error.ToString());
    }

    private static void AssertNoCrash(string text)
    {
        Assert.DoesNotContain(CrashMarker, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(TraceMarker, text, StringComparison.Ordinal);
    }

    private static string WriteYaml(string dir, string content)
    {
        var path = Path.Combine(dir, "case.yaml");
        File.WriteAllText(path, content);
        return path;
    }

    private static string Header(string name = "edge", int repeats = 1, int timeout = 30, int warmup = 0) =>
        $"name: {name}\ncommand: dotnet\nrepeats: {repeats}\ntimeoutSeconds: {timeout}\nwarmup: {warmup}\n";

    // Case 1: input validation

    [Fact]
    public async Task Validation_AllFieldsInvalid_ReportsEveryField()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = WriteYaml(dir, "name: ''\ncommand: ''\nrepeats: 0\ntimeoutSeconds: 0\nwarmup: -1\n");

            var result = await Invoke(new[] { "run", path });

            Assert.Equal(CliApp.ExitUsage, result.Code);
            foreach (var field in new[] { "name", "command", "repeats", "timeoutSeconds", "warmup" })
            {
                Assert.Contains(field, result.Error, StringComparison.Ordinal);
            }

            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void Validation_NullArgument_IsReported()
    {
        var error = Assert.Throws<ScenarioException>(
            () => ScenarioLoader.Parse("name: a\ncommand: dotnet\nargs: [~, ok]\n"));

        Assert.Contains("args", error.Message, StringComparison.Ordinal);
    }

    // Case 2: empty and malformed data

    [Theory]
    [InlineData("")]
    [InlineData("   \n\n")]
    [InlineData("# тільки коментар\n")]
    [InlineData("[]")]
    [InlineData("just text")]
    [InlineData("name")]
    [InlineData("name: [unclosed")]
    [InlineData("name: a\ncommand: dotnet\nrepeats: many\n")]
    [InlineData("name: a\ncommand: dotnet\nargs: not-a-list\n")]
    [InlineData("name: a\ncommand: dotnet\nrepeats: 99999999999999999999\n")]
    [InlineData("\t\tname: a")]
    public async Task MalformedScenario_ReturnsUsageCodeWithoutStackTrace(string content)
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var result = await Invoke(new[] { "run", WriteYaml(dir, content) });

            Assert.Equal(CliApp.ExitUsage, result.Code);
            Assert.NotEmpty(result.Error);
            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task BinaryGarbageInsteadOfScenario_ReturnsUsageCode()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = Path.Combine(dir, "garbage.yaml");
            await File.WriteAllBytesAsync(path, new byte[] { 0, 159, 146, 150, 255, 254, 0, 1, 2, 3 });

            var result = await Invoke(new[] { "run", path });

            Assert.Equal(CliApp.ExitUsage, result.Code);
            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task DirectoryInsteadOfScenarioFile_ReturnsUsageCode()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var result = await Invoke(new[] { "run", dir });

            Assert.Equal(CliApp.ExitUsage, result.Code);
            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void EmptyArgsList_IsTreatedAsNoArguments()
    {
        var scenario = ScenarioLoader.Parse("name: a\ncommand: dotnet\nargs:\n");

        Assert.NotNull(scenario.Args);
        Assert.Empty(scenario.Args);
    }

    [Theory]
    [InlineData("run")]
    [InlineData("send")]
    [InlineData("stop", "file.yaml")]
    [InlineData("run", "file.yaml", "--server")]
    [InlineData("run", "file.yaml", "--server", "not-a-url")]
    [InlineData("run", "file.yaml", "--out", " ")]
    [InlineData("run", "file.yaml", "--unknown", "1")]
    public async Task WrongCommandLine_PrintsUsageAndReturnsUsageCode(params string[] args)
    {
        var result = await Invoke(args);

        Assert.Equal(CliApp.ExitUsage, result.Code);
        Assert.Contains("Використання", result.Error, StringComparison.Ordinal);
        AssertNoCrash(result.Error);
    }

    [Fact]
    public async Task SendFromEmptyFolder_ReportsNothingToSend()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var result = await Invoke(new[] { "send", dir, "--server", "http://localhost:5000" });

            Assert.Equal(CliApp.ExitOk, result.Code);
            Assert.Contains("Немає файлів", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    // Case 3: exceptions without a crash

    [Fact]
    public async Task UnknownProgram_IsRecordedAsFailedMeasurement()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = WriteYaml(dir, "name: ghost\ncommand: definitely-not-a-program-xyz\nrepeats: 2\ntimeoutSeconds: 5\n");

            var result = await Invoke(new[] { "run", path });

            Assert.Equal(CliApp.ExitOk, result.Code);
            Assert.Contains("Failed", result.Output, StringComparison.Ordinal);
            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task MissingWorkingDirectory_IsRecordedAsFailedMeasurement()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var missing = Path.Combine(dir, "no-such-folder").Replace("\\", "/");
            var path = WriteYaml(dir, Header() + $"workingDirectory: '{missing}'\n");

            var result = await Invoke(new[] { "run", path });

            Assert.Equal(CliApp.ExitOk, result.Code);
            Assert.Contains("Failed", result.Output, StringComparison.Ordinal);
            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task UnwritableOutPath_ReturnsInternalCodeWithMessage()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0", repeats: 1);
            var blocker = Path.Combine(dir, "blocker");
            await File.WriteAllTextAsync(blocker, "file, not a folder");

            var result = await Invoke(new[] { "run", scenario, "--out", Path.Combine(blocker, "result.json") });

            Assert.Equal(CliApp.ExitInternal, result.Code);
            Assert.Contains("Помилка виконання", result.Error, StringComparison.Ordinal);
            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task ServerDownAndUnwritableResultsDir_ReturnsInternalCode()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0", repeats: 1);
            var blocker = Path.Combine(dir, "blocker");
            await File.WriteAllTextAsync(blocker, "file, not a folder");
            var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

            var result = await Invoke(
                new[] { "run", scenario, "--server", "http://localhost:5999", "--results-dir", Path.Combine(blocker, "sub") },
                handler);

            Assert.Equal(CliApp.ExitInternal, result.Code);
            Assert.Contains("Помилка виконання", result.Error, StringComparison.Ordinal);
            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Theory]
    [InlineData("not json at all", "application/json")]
    [InlineData("{\"runId\":", "application/json")]
    [InlineData("<html>ok</html>", "text/html")]
    public async Task ServerAnswersWithGarbage_IsTreatedAsFailedSend(string body, string mediaType)
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var scenario = CliTestHelpers.WriteScenario(dir, "0", repeats: 1);
            var results = Path.Combine(dir, "saved");
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(body, Encoding.UTF8, mediaType)
            });

            var result = await Invoke(
                new[] { "run", scenario, "--server", "http://localhost:5999", "--results-dir", results },
                handler);

            Assert.Equal(CliApp.ExitSendFailed, result.Code);
            Assert.Single(Directory.GetFiles(results, "*.json"));
            AssertNoCrash(result.Error);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    // Case 4: boundary values

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(100, 3600, 0)]
    [InlineData(1, 30, 5)]
    public void BoundaryValues_AreAccepted(int repeats, int timeout, int warmup)
    {
        var scenario = ScenarioLoader.Parse(Header(repeats: repeats, timeout: timeout, warmup: warmup));

        Assert.Equal(repeats, scenario.Repeats);
        Assert.Equal(timeout, scenario.TimeoutSeconds);
        Assert.Equal(warmup, scenario.Warmup);
    }

    [Theory]
    [InlineData(0, 30, 0)]
    [InlineData(101, 30, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 3601, 0)]
    [InlineData(1, 30, -1)]
    public void OutsideBoundary_IsRejected(int repeats, int timeout, int warmup)
    {
        Assert.Throws<ScenarioException>(
            () => ScenarioLoader.Parse(Header(repeats: repeats, timeout: timeout, warmup: warmup)));
    }

    [Fact]
    public void ScenarioName_Of100CharactersIsAccepted_And101IsRejected()
    {
        var accepted = ScenarioLoader.Parse(Header(name: new string('a', 100)));

        Assert.Equal(100, accepted.Name.Length);
        Assert.Throws<ScenarioException>(() => ScenarioLoader.Parse(Header(name: new string('a', 101))));
    }

    [Fact]
    public async Task OneSecondTimeout_StopsLongProgramAndMarksTimeout()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = WriteYaml(dir, $"name: slow\ncommand: dotnet\nargs: ['{CliTestHelpers.HelperDll}', 'sleep', '30']\nrepeats: 1\ntimeoutSeconds: 1\n");
            var watch = Stopwatch.StartNew();

            var result = await Invoke(new[] { "run", path });

            watch.Stop();
            Assert.Equal(CliApp.ExitOk, result.Code);
            Assert.Contains("Timeout", result.Output, StringComparison.Ordinal);
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), "процес не був зупинений за таймаутом");
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    // Case 5: no unhandled exceptions in the run log

    [Fact]
    public async Task ManyBadInvocations_LeaveNoStackTracesInOutput()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var combined = new StringBuilder();
            var invocations = new List<string[]>
            {
                Array.Empty<string>(),
                new[] { "run" },
                new[] { "run", Path.Combine(dir, "missing.yaml") },
                new[] { "run", dir },
                new[] { "run", WriteYaml(dir, "[]") },
                new[] { "send", Path.Combine(dir, "missing"), "--server", "http://localhost:5000" },
                new[] { "run", CliTestHelpers.WriteScenario(dir, "3", repeats: 1) }
            };

            foreach (var args in invocations)
            {
                var result = await Invoke(args);
                combined.AppendLine(result.Output).AppendLine(result.Error);
            }

            AssertNoCrash(combined.ToString());
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public async Task RealProcess_WithMissingScenario_ExitsWithUsageCodeAndNoCrashReport()
    {
        var runner = Path.Combine(AppContext.BaseDirectory, "PerfCompare.Runner.dll");
        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        info.ArgumentList.Add(runner);
        info.ArgumentList.Add("run");
        info.ArgumentList.Add(Path.Combine(Path.GetTempPath(), "perfcompare-missing-" + Guid.NewGuid().ToString("N") + ".yaml"));

        using var process = Process.Start(info)!;
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Equal(CliApp.ExitUsage, process.ExitCode);
        AssertNoCrash(error);
    }
}
