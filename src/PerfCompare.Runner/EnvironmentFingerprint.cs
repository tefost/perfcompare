using System.Runtime.InteropServices;
using PerfCompare.Contracts;

namespace PerfCompare.Runner;

public static class EnvironmentFingerprint
{
    public static EnvironmentInfo Capture()
    {
        var ramMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);

        return new EnvironmentInfo(
            DetectOs(),
            Environment.OSVersion.Version.ToString(),
            RuntimeInformation.FrameworkDescription,
            Math.Max(1, Environment.ProcessorCount),
            Math.Max(1L, ramMb));
    }

    private static string DetectOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Unknown";
    }
}
