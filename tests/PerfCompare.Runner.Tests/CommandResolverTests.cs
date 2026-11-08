using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class CommandResolverTests
{
    private static string TempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "perfcompare-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTool(string dir, string relativeName)
    {
        var path = Path.Combine(dir, relativeName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "tool");
        return path;
    }

    [Theory]
    [InlineData("dotnet")]
    [InlineData("python3")]
    [InlineData("tool.exe")]
    public void Resolve_NameWithoutDirectory_IsReturnedAsIs(string command)
    {
        Assert.Equal(command, CommandResolver.Resolve(command, Path.GetTempPath()));
    }

    [Fact]
    public void Resolve_RelativePathWithSlashes_BecomesAbsolutePath()
    {
        var dir = TempDir();
        try
        {
            var tool = CreateTool(dir, Path.Combine("samples", "bin", "CpuBound"));
            var command = string.Join(Path.AltDirectorySeparatorChar, "samples", "bin", "CpuBound");

            var resolved = CommandResolver.Resolve(command, dir);

            Assert.Equal(tool, resolved);
            Assert.True(Path.IsPathRooted(resolved));
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Resolve_MissingFile_ReturnsOriginalCommand()
    {
        var dir = TempDir();
        try
        {
            var command = string.Join(Path.AltDirectorySeparatorChar, "samples", "bin", "Missing");

            Assert.Equal(command, CommandResolver.Resolve(command, dir));
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Resolve_AbsolutePath_StaysTheSame()
    {
        var dir = TempDir();
        try
        {
            var tool = CreateTool(dir, "absolute-tool");

            Assert.Equal(tool, CommandResolver.Resolve(tool, Path.GetTempPath()));
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Resolve_OnWindows_AddsExeExtension()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var dir = TempDir();
        try
        {
            var tool = CreateTool(dir, Path.Combine("bin", "CpuBound.exe"));
            var command = string.Join(Path.AltDirectorySeparatorChar, "bin", "CpuBound");

            Assert.Equal(tool, CommandResolver.Resolve(command, dir));
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Resolve_OnUnix_DoesNotAddExeExtension()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var dir = TempDir();
        try
        {
            CreateTool(dir, Path.Combine("bin", "CpuBound.exe"));
            var command = string.Join(Path.AltDirectorySeparatorChar, "bin", "CpuBound");

            Assert.Equal(command, CommandResolver.Resolve(command, dir));
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public void Resolve_UsesCurrentDirectoryByDefault()
    {
        Assert.Equal("dotnet", CommandResolver.Resolve("dotnet"));
    }
}
