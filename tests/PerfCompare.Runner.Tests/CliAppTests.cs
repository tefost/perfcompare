using System.Net;
using System.Text;
using System.Text.Json;
using PerfCompare.Contracts;
using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class CliAppTests
{
    private static readonly string HelperDll =
        Path.Combine(AppContext.BaseDirectory, "PerfCompare.TestApp.dll");

    private static string TempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "perfcompare-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string WriteScenario(string dir, string exitCode)
    {
        var path = Path.Combine(dir, "scenario.yaml");
        File.WriteAllText(path, $"""
            name: cli-test
            command: dotnet
            args: ['{HelperDll}', 'exit', '{exitCode}']
            repeats: 2
            timeoutSeconds: 20
            """);
        return path;
    }

    private static async Task<(int Code, string Out, string Err)> Run(
        string[] args, HttpMessageHandler? handler = null)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = await CliApp.RunAsync(args, output, error, handler);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public async Task Run_InvalidYaml_ReturnsTwoWithReadableMessage()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "bad.yaml");
            await File.WriteAllTextAsync(path, "name: [unclosed");

            var result = await Run(new[] { "run", path });

            Assert.Equal(CliApp.ExitUsage, result.Code);
            Assert.Contains("Помилка сценарію", result.Err);
            Assert.DoesNotContain("   at ", result.Err);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_InvalidFields_ReturnsTwoAndNamesTheField()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "bad.yaml");
            await File.WriteAllTextAsync(path, "name: x\ncommand: dotnet\nrepeats: 101\n");

            var result = await Run(new[] { "run", path });

            Assert.Equal(CliApp.ExitUsage, result.Code);
            Assert.Contains("repeats", result.Err);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_MissingFile_ReturnsTwo()
    {
        var result = await Run(new[] { "run", Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".yaml") });

        Assert.Equal(CliApp.ExitUsage, result.Code);
        Assert.Contains("не знайдено", result.Err);
    }

    [Fact]
    public async Task Run_WrongUsage_ReturnsTwoAndPrintsUsage()
    {
        var result = await Run(new[] { "start" });

        Assert.Equal(CliApp.ExitUsage, result.Code);
        Assert.Contains("Використання", result.Err);
    }

    [Fact]
    public async Task Run_WithOut_WritesResultFile()
    {
        var dir = TempDir();
        try
        {
            var scenario = WriteScenario(dir, "0");
            var outPath = Path.Combine(dir, "out", "result.json");

            var result = await Run(new[] { "run", scenario, "--out", outPath });

            Assert.Equal(CliApp.ExitOk, result.Code);
            var loaded = JsonSerializer.Deserialize<RunResult>(await File.ReadAllTextAsync(outPath), ContractJson.Options);
            Assert.Equal(2, loaded!.Measurements.Count);
            Assert.All(loaded.Measurements, m => Assert.Equal(MeasurementStatus.Passed, m.Status));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_WithServer_SendsResultAndPrintsRunId()
    {
        var dir = TempDir();
        try
        {
            var scenario = WriteScenario(dir, "0");
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"runId\":7}", Encoding.UTF8, "application/json")
            });

            var result = await Run(new[] { "run", scenario, "--server", "http://localhost:5000" }, handler);

            Assert.Equal(CliApp.ExitOk, result.Code);
            Assert.Contains("runId=7", result.Out);
            Assert.Single(handler.Bodies);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_ServerFails_ReturnsOneWithMessage()
    {
        var dir = TempDir();
        try
        {
            var scenario = WriteScenario(dir, "0");
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var result = await Run(new[] { "run", scenario, "--server", "http://localhost:5000" }, handler);

            Assert.Equal(CliApp.ExitSendFailed, result.Code);
            Assert.Contains("Не вдалося відправити", result.Err);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_FailingApp_StillReturnsOkAndKeepsFailedStatus()
    {
        var dir = TempDir();
        try
        {
            var scenario = WriteScenario(dir, "3");
            var outPath = Path.Combine(dir, "result.json");

            var result = await Run(new[] { "run", scenario, "--out", outPath });

            Assert.Equal(CliApp.ExitOk, result.Code);
            var loaded = JsonSerializer.Deserialize<RunResult>(await File.ReadAllTextAsync(outPath), ContractJson.Options);
            Assert.All(loaded!.Measurements, m => Assert.Equal(MeasurementStatus.Failed, m.Status));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_NoServerNoOut_PrintsJsonToOutput()
    {
        var dir = TempDir();
        try
        {
            var scenario = WriteScenario(dir, "0");

            var result = await Run(new[] { "run", scenario });

            Assert.Equal(CliApp.ExitOk, result.Code);
            Assert.Contains("\"measurements\"", result.Out);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
