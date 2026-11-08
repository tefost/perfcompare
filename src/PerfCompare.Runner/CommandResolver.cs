namespace PerfCompare.Runner;

public static class CommandResolver
{
    public static string Resolve(string command)
    {
        return Resolve(command, Directory.GetCurrentDirectory());
    }

    public static string Resolve(string command, string baseDirectory)
    {
        if (!HasDirectory(command))
        {
            return command;
        }

        var normalized = command.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(normalized, baseDirectory);
        if (File.Exists(full))
        {
            return full;
        }

        var withExtension = full + ".exe";
        return OperatingSystem.IsWindows() && File.Exists(withExtension) ? withExtension : command;
    }

    private static bool HasDirectory(string command)
    {
        return command.Contains(Path.DirectorySeparatorChar) || command.Contains(Path.AltDirectorySeparatorChar);
    }
}
