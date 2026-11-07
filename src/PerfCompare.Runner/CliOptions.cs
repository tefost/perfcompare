namespace PerfCompare.Runner;

public sealed record CliOptions(string ScenarioPath, string? ServerUrl, string? OutPath)
{
    public const string Usage =
        "Використання: PerfCompare.Runner run <scenario.yaml> [--server <url>] [--out <файл>]";

    public static bool TryParse(string[] args, out CliOptions? options, out string error)
    {
        options = null;
        error = string.Empty;

        if (args.Length < 2 || args[0] != "run")
        {
            error = "Очікується команда: run <scenario.yaml>";
            return false;
        }

        string? server = null;
        string? outPath = null;

        var index = 2;
        while (index < args.Length)
        {
            var name = args[index];
            var value = index + 1 < args.Length ? args[index + 1] : null;

            if (name == "--server")
            {
                if (value is null || !IsHttpUrl(value))
                {
                    error = "--server потребує адреси виду http://localhost:5000";
                    return false;
                }

                server = value;
            }
            else if (name == "--out")
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    error = "--out потребує шляху до файлу";
                    return false;
                }

                outPath = value;
            }
            else
            {
                error = $"Невідомий параметр: {name}";
                return false;
            }

            index += 2;
        }

        options = new CliOptions(args[1], server, outPath);
        return true;
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
