namespace PerfCompare.Runner;

public sealed record CliOptions(string Command, string ScenarioPath, string? ServerUrl, string? OutPath, string ResultsDir)
{
    public const string RunCommand = "run";
    public const string SendCommand = "send";
    public const string DefaultResultsDir = "results";

    public const string Usage =
        "Використання:\n"
        + "  PerfCompare.Runner run <scenario.yaml> [--server <url>] [--out <файл>] [--results-dir <папка>]\n"
        + "  PerfCompare.Runner send <файл|папка> --server <url>";

    public static bool TryParse(string[] args, out CliOptions? options, out string error)
    {
        options = null;
        error = string.Empty;

        if (args.Length < 2 || (args[0] != RunCommand && args[0] != SendCommand))
        {
            error = "Очікується команда: run <scenario.yaml> або send <файл|папка>";
            return false;
        }

        var parsed = new ParsedOptions();
        var index = 2;
        while (index < args.Length)
        {
            var value = index + 1 < args.Length ? args[index + 1] : null;
            var problem = Apply(parsed, args[index], value);
            if (problem is not null)
            {
                error = problem;
                return false;
            }

            index += 2;
        }

        if (args[0] == SendCommand && (parsed.Server is null || parsed.Out is not null || parsed.ResultsDir is not null))
        {
            error = "send потребує --server <url> і не приймає --out та --results-dir";
            return false;
        }

        options = new CliOptions(args[0], args[1], parsed.Server, parsed.Out, parsed.ResultsDir ?? DefaultResultsDir);
        return true;
    }

    private static string? Apply(ParsedOptions parsed, string name, string? value)
    {
        switch (name)
        {
            case "--server":
                if (value is null || !IsHttpUrl(value))
                {
                    return "--server потребує адреси виду http://localhost:5000";
                }

                parsed.Server = value;
                return null;
            case "--out":
                if (string.IsNullOrWhiteSpace(value))
                {
                    return "--out потребує шляху до файлу";
                }

                parsed.Out = value;
                return null;
            case "--results-dir":
                if (string.IsNullOrWhiteSpace(value))
                {
                    return "--results-dir потребує шляху до папки";
                }

                parsed.ResultsDir = value;
                return null;
            default:
                return $"Невідомий параметр: {name}";
        }
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private sealed class ParsedOptions
    {
        public string? Server { get; set; }

        public string? Out { get; set; }

        public string? ResultsDir { get; set; }
    }
}
