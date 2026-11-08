using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class ResultStoreTests
{
    [Fact]
    public void Save_CreatesFileWithScenarioNameAndTimestamp()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = ResultStore.Save(CliTestHelpers.SampleRun("baseline"), Path.Combine(dir, "results"));

            Assert.True(File.Exists(path));
            Assert.StartsWith("baseline-20261108T100000000", Path.GetFileName(path));
            Assert.EndsWith(".json", path);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void Save_UnsafeScenarioName_IsSanitized()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = ResultStore.Save(CliTestHelpers.SampleRun("a/b:c*d"), dir);

            Assert.True(File.Exists(path));
            Assert.DoesNotContain("/", Path.GetFileName(path));
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void Save_ThenTryLoad_ReturnsSameRun()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var original = CliTestHelpers.SampleRun();
            var path = ResultStore.Save(original, dir);

            var loaded = ResultStore.TryLoad(path);

            Assert.NotNull(loaded);
            Assert.Equal(original.Scenario, loaded!.Scenario);
            Assert.Equal(original.Environment, loaded.Environment);
            Assert.Single(loaded.Measurements);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void ListPending_Directory_ReturnsJsonFilesSorted()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            ResultStore.Save(CliTestHelpers.SampleRun("b", minute: 5), dir);
            ResultStore.Save(CliTestHelpers.SampleRun("a", minute: 1), dir);
            File.WriteAllText(Path.Combine(dir, "notes.txt"), "x");

            var files = ResultStore.ListPending(dir);

            Assert.Equal(2, files!.Count);
            Assert.StartsWith("a-", Path.GetFileName(files[0]));
            Assert.StartsWith("b-", Path.GetFileName(files[1]));
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void ListPending_SingleFile_ReturnsThatFile()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = ResultStore.Save(CliTestHelpers.SampleRun(), dir);

            var files = ResultStore.ListPending(path);

            Assert.Single(files!);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void ListPending_MissingPath_ReturnsNull()
    {
        Assert.Null(ResultStore.ListPending(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
    }

    [Fact]
    public void MarkSent_RenamesFileAndExcludesItFromPending()
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = ResultStore.Save(CliTestHelpers.SampleRun(), dir);

            ResultStore.MarkSent(path);

            Assert.False(File.Exists(path));
            Assert.True(File.Exists(path + ResultStore.SentSuffix));
            Assert.Empty(ResultStore.ListPending(dir)!);
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{}")]
    [InlineData("null")]
    public void TryLoad_BrokenOrForeignContent_ReturnsNull(string content)
    {
        var dir = CliTestHelpers.TempDir();
        try
        {
            var path = Path.Combine(dir, "bad.json");
            File.WriteAllText(path, content);

            Assert.Null(ResultStore.TryLoad(path));
        }
        finally
        {
            CliTestHelpers.Cleanup(dir);
        }
    }

    [Fact]
    public void TryLoad_MissingFile_ReturnsNull()
    {
        Assert.Null(ResultStore.TryLoad(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json")));
    }
}
