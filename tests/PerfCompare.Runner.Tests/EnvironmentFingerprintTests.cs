using PerfCompare.Runner;

namespace PerfCompare.Runner.Tests;

public class EnvironmentFingerprintTests
{
    [Fact]
    public void Capture_FillsAllFields()
    {
        var info = EnvironmentFingerprint.Capture();

        Assert.False(string.IsNullOrWhiteSpace(info.Os));
        Assert.False(string.IsNullOrWhiteSpace(info.OsVersion));
        Assert.False(string.IsNullOrWhiteSpace(info.Runtime));
        Assert.True(info.Cores >= 1);
        Assert.True(info.RamMb >= 1);
    }

    [Fact]
    public void Capture_OsIsOneOfKnownNames()
    {
        var info = EnvironmentFingerprint.Capture();

        Assert.Contains(info.Os, new[] { "Windows", "Linux", "macOS", "Unknown" });
    }

    [Fact]
    public void Capture_CalledTwice_ReturnsSameFingerprint()
    {
        var first = EnvironmentFingerprint.Capture();
        var second = EnvironmentFingerprint.Capture();

        Assert.Equal(first.Os, second.Os);
        Assert.Equal(first.OsVersion, second.OsVersion);
        Assert.Equal(first.Runtime, second.Runtime);
        Assert.Equal(first.Cores, second.Cores);
    }
}
